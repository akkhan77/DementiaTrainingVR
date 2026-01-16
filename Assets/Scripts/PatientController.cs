using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.Animations.Rigging;
using System;

public class PatientController : MonoBehaviour
{
    [SerializeField] private Transform _XrRigTransform;
    [SerializeField] public Animator _animator;
    [SerializeField] private Transform _patientLyingTransform;
    [SerializeField] private Transform _patientSitTransform;
    [SerializeField] private SkinnedMeshRenderer _faceRenderer;
    [SerializeField] private PatientFacialController _patientFacialController;
    [SerializeField] private AIFeedbackController _aiFeedback;
    [SerializeField] private AudioSource _patientAudioSource;
    [SerializeField] private AudioClip _patientAngryClip;
    [SerializeField] private AudioClip _patientCalmClip;
    [SerializeField] private GameObject[] _patientWires;

    [Header("Talking Settings")]
    [SerializeField] private float _talkSpeed = 4f;

    [Header("Blinking Settings")]
    [SerializeField] private float _blinkInterval = 1f;   // time between blinks
    [SerializeField] private float _blinkSpeed = 10f;
   
    //-0.864
    [Header("Rig Settings")]
    [SerializeField] private RigBuilder _rigBuilder;
    [SerializeField] private Transform _spineTargetTransform;
    [SerializeField] private Transform _HipTargetTransform;

    [Header("Walking Settings")]
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private Transform _bedPoint;
    [SerializeField] private GameObject _bedSuitcase;
    [SerializeField] private GameObject _patientSuitcase;
    [SerializeField] private Transform[] _wanderPoints;

    int _currentIndex = 0;
    bool _isWandering = false;
    public bool _isTalking = false;
    private bool _blink = false;
    private bool _idleAnim = false;

    private int _mouthBlendShapeIndex;
    private int _eyesBlendShapeIndex;
    private float _blinkTimer;
    private float _blinkValue;
    private bool _isBlinking;
    private bool _hasArrived;
    private Transform _currentTarget;

    public Animator animator; // Inspector mein Animator assign karein
    public Vector3 newPosition = new Vector3(5, 0, 0); // Jahan move karna hai
    public Vector3 newRotation = new Vector3(0, 90, 0); // Jitna rotate karna ha
    private bool _isLookingAtObject = false;
    public PatientFacialController FacialController => _patientFacialController;
    [Header("Look At Clock Settings")]
    [SerializeField] private MultiAimConstraint _neckAimConstraint;
    [SerializeField] private Transform _clockTransform, _hospitalreg, _familyphoto;
    [Header("Video Call Settings")]
    [SerializeField] private TwoBoneIKConstraint _leftHandIK;
    [SerializeField] private Transform _mobileTarget;
    void Start()
    {
        _mouthBlendShapeIndex = _faceRenderer.sharedMesh.GetBlendShapeIndex("talk");
        _eyesBlendShapeIndex = _faceRenderer.sharedMesh.GetBlendShapeIndex("eyeclose");
    }

    void Update()
    {
        HandleTalking();
        HandleWalking();
        HandleBlinking();
        //LookAtClock(true);


        if (_idleAnim ) // Sirf tab FacePlayer chale jab hum kisi object ko na dekh rahe hon
        {
            HandleReachedDestination();
        }
    }
    public void CallMobilePose()
    {
        // 1. Mobile model ko active karein
        if (GameController.instance.mobilemodel != null)
            GameController.instance.mobilemodel.SetActive(true);

        // 2. TARGET POSITION (Ise foran set karein taake IK sahi raste par chale)
        Vector3 videoCallPos = new Vector3(-0.135f, 0.416f, 0.318f);
        Vector3 videoCallRot = new Vector3(-15.412f, 63.972f, 100.992f);

        // Isay fast move karein taake hath uthne se pehle target tayyar ho
        _mobileTarget.DOLocalMove(videoCallPos, 0.5f).SetEase(Ease.Linear);
        _mobileTarget.DOLocalRotate(videoCallRot, 0.5f).SetEase(Ease.Linear);

        // 3. SMOOTH IK WEIGHT (Slow start ke liye Ease.InOutSine use kiya hai)
        // Duration ko 2.0s kar diya hai taake jhatka bilkul mehsoos na ho
        _leftHandIK.weight = 0f;
        DOTween.To(() => _leftHandIK.weight, x => _leftHandIK.weight = x, 1f, 2.0f)
            .SetEase(Ease.InOutSine)
            .OnUpdate(() => {
                _rigBuilder.Build(); // Sync animator and rig
        });

        // 4. Neck Movement (Hath upar pohonchne ke baad)
        DOVirtual.DelayedCall(1.5f, () => {
            LookAtTargetByIndex(0); // Look at mobarea
        });
    }
    public void StartMobileVideoCall()
    {
        // 1. Mobile model ko active karein
        if (GameController.instance.mobilemodel != null)
            GameController.instance.mobilemodel.SetActive(true);

        // 2. TARGET POSITION & ROTATION (Jo aapne image mein di hain)
        // In values ko hum LocalSpace mein set karenge taake ye hamesha patient ke mutabiq rahein
        Vector3 newPos = new Vector3(-0.292f, 0.368f, 0.241f);
        Vector3 newRot = new Vector3(-29.886f, 188.209f, 81.757f);

        // Smooth transition ke liye DOTween use karein
        _mobileTarget.DOLocalMove(newPos, 1.2f).SetEase(Ease.OutQuad);
        _mobileTarget.DOLocalRotate(newRot, 1.2f).SetEase(Ease.OutQuad);

        // 3. Left Hand IK Weight smoothly 1 karein
        _leftHandIK.weight = 0f;
        DOTween.To(() => _leftHandIK.weight, x => _leftHandIK.weight = x, 1f, 1.5f)
            .SetEase(Ease.OutCubic)
            .OnUpdate(() => {
                _rigBuilder.Build(); // Hath ko force refresh karein
            });

        // 4. Neck Movement (Mobile ki taraf dekhne ke liye)
        DOVirtual.DelayedCall(1.0f, () => {
            LookAtTwoTargets(0, 2);
            // Ensure karein ke target 0 mobarea hi ho
        });
    }
    // Mobile khatam karne ke liye function
    public void StopMobilePose()
    {
        DOTween.To(() => _leftHandIK.weight, x => _leftHandIK.weight = x, 0f, 0.8f)
            .OnUpdate(() => _rigBuilder.Build());

        if (GameController.instance.mobilemodel != null)
            GameController.instance.mobilemodel.SetActive(false);
    }
    private void HandleTalking()
    {
        if (_isTalking)
        {
            float t = Mathf.PingPong(Time.time * _talkSpeed, 1f);
            float mouth = Mathf.Lerp(20f, 80f, t);

            _faceRenderer.SetBlendShapeWeight(_mouthBlendShapeIndex, mouth);
        }
        else
        {
            _faceRenderer.SetBlendShapeWeight(_mouthBlendShapeIndex, 0);
        }
    }

    private void HandleWalking()
    {
        if (!_isWandering) return;

        if (!_agent.pathPending && _agent.remainingDistance < 0.3f)
        {
            _currentIndex = (_currentIndex + 1) % _wanderPoints.Length;
            _agent.SetDestination(_wanderPoints[_currentIndex].position);
        }
    }

    private void HandleBlinking()
    {
        if (!_blink)
        {
            return;
        }

        _blinkTimer += Time.deltaTime;

        // Start blink every interval
        if (_blinkTimer >= _blinkInterval)
        {
            _isBlinking = true;
            _blinkTimer = 0f;
        }

        if (_isBlinking)
        {
            // Close eyes
            _blinkValue = Mathf.MoveTowards(_blinkValue, 100f, _blinkSpeed * 100f * Time.deltaTime);
            _faceRenderer.SetBlendShapeWeight(_eyesBlendShapeIndex, _blinkValue);

            // When fully closed → start opening
            if (_blinkValue >= 100f)
            {
                _isBlinking = false;
            }
        }
        else
        {
            // Open eyes
            _blinkValue = Mathf.MoveTowards(_blinkValue, 0f, _blinkSpeed * 100f * Time.deltaTime);
            _faceRenderer.SetBlendShapeWeight(_eyesBlendShapeIndex, _blinkValue);
        }
    }
    [ContextMenu("Smooth Rotate Patient")]
    public void RotatePatientSmoothly()
    {

        Vector3 targetEuler = new Vector3(0f, -365f, 1.118f);
        LookAtTwoTargets(1, 3);
        // DORotateQuaternion use karne se 'flipping' ka masla hal ho jata hai
        // Hum direct Quaternion calculate kar rahe hain taake wo aaram se target par jaye
        transform.DORotateQuaternion(Quaternion.Euler(targetEuler), 1.5f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() => {
                Debug.Log("Rotation Fixed without flipping!");
            });
        _animator.SetBool("onbed", true);
      
    }
    private void HandleReachedDestination()
    {
        if (_currentTarget == null || _hasArrived)
            return;

        // Wait until path is calculated
        if (_agent.pathPending)
            return;

        // Agent has stopped moving → ARRIVED
        if (_agent.velocity.sqrMagnitude < 0.05f)
        {
            OnArrived();
        }
    }

    private void OnArrived()
    {
        _hasArrived = true;

        _agent.isStopped = true;
        _agent.ResetPath();

        _animator.SetBool("Idle", true);
        FacePlayer();
    }

    private void FacePlayer()
    {
        transform.LookAt(_XrRigTransform);
    }

    private void GoToPosition(Transform transform)
    {
        _isWandering = false;
        _hasArrived = false;
        _currentTarget = transform;

        _agent.isStopped = false;
        _agent.SetDestination(transform.position);
    }

    [ContextMenu("Make Aggressive")]
    public void Aggressive()
    {

        _blink = true;
        //_spineTargetTransform.DOLocalRotate(new Vector3(70, -22, 0), 1f).SetEase(Ease.OutExpo);
        //_HipTargetTransform.DOLocalRotate(new Vector3(0, -3, 0), 1f).SetEase(Ease.OutExpo);
        _aiFeedback.ConversationCanvas.SetActive(false);

        _patientFacialController.SetExpression(ExpressionType.Aggressive);
    
        StartCoroutine(Confusion());
    }

    private IEnumerator Confusion()
    {
        transform.position = newPosition;

        transform.rotation = Quaternion.Euler(newRotation);

        DisablePatientWires();

        if (_agent != null)

        {

            _agent.enabled = false; // Pehle disable karein

            _agent.Warp(transform.position); // Ye agent ko current position par 'snap' kar deta hai

            //_agent.enabled = true; // Phir enable karein

        }

        // 2. Wakeup Animation chalayein

        _animator.Play("wakeup");

        // 3. Thoda wait karein taake animation mehsoos ho (e.g., 2 seconds)

        yield return new WaitForSeconds(18.10f);
        HandleSuitCase(false);
        yield return new WaitForSeconds(1.11f);
        _agent.enabled = true;
        DisablePatientWires();
        _isWandering = true;
        _animator.SetBool("Walk", true);
        yield return new WaitForSeconds(1f);
        StartCoroutine(HitAnimationSequence());
        yield return new WaitForSeconds(4f);
        _animator.SetBool("hit", false);
        yield return new WaitForSeconds(5f);
        Debug.Log("22222");
        _idleAnim = true;
        GoToPosition(_XrRigTransform);
        yield return new WaitForSeconds(3f);
        Debug.Log("33333");
        HandlePatientNeck(true);
        LookAtTargetByIndex(0);
        _isTalking = true;
        _aiFeedback.ConversationCanvas.SetActive(true);
        _aiFeedback.SetConversationText("대상자의 증상 관찰 및 환자행동을확인하세요.");
        _patientFacialController.SetExpression(ExpressionType.Aggressive);
        _patientAudioSource.clip = _patientAngryClip;
        _patientAudioSource.Play();
        yield return new WaitForSeconds(_patientAngryClip.length);
        Debug.Log("44444");
        _isTalking = false;
        yield return new WaitForSeconds(2f);
        Debug.Log("55555");
        _aiFeedback.SetConversationText("집에 가고 싶으신 거군요.\n그 전에, 잠시\n앉아서 저와 잠깐 이야기 좀 나눠주시겠어요?");
        _aiFeedback.StartListening(_aiFeedback.AiFeedbackClip02);
        _aiFeedback.StartBlinking();
        yield return new WaitForSeconds(2f);
        Debug.Log("66666");
        HandlePatientNeck(false);
        _animator.SetBool("Idle", false);
        _idleAnim = false;
        yield return new WaitForSeconds(2f);
        GoToPosition(_bedPoint);
    }

    // PatientController.cs ke andar aakhir mein:

    IEnumerator HitAnimationSequence()
    {
        _agent.updateRotation = false;
        transform.DOLookAt(Camera.main.transform.position, 0.5f, AxisConstraint.Y);
        _animator.SetBool("hit", true);

        // Hit impact ka intezar
        yield return new WaitForSeconds(0.4f);

        // VR ke liye: Camera ke parent (XR Origin/Camera Offset) ko dhoondhein
        Transform cameraParent = Camera.main.transform.parent;

        if (cameraParent != null)
        {
            // Heavy Shake: Strength 1.5 aur Vibrato 50
            cameraParent.DOShakePosition(0.5f, 1.5f, 50, 90, false, true);

            // Rotation shake ke liye agar cameraParent rotation allow karta hai
            cameraParent.DOShakeRotation(0.5f, 7f, 30);
        }
        else
        {
            // Agar parent nahi hai, to seedha camera ko force karein (lekin parent behtar hai)
            Camera.main.transform.DOShakePosition(0.5f, 1.5f, 50).SetRelative(true);
        }

        yield return new WaitForSeconds(1.5f);
        _agent.updateRotation = true;
        _animator.SetBool("hit", false);
    }
    public void PatientSitDown()
    {
        _agent.enabled = false;
        HandleSuitCase(true);
        DisablePatientWires();
        _patientFacialController.SetExpression(ExpressionType.Confusion);

        _spineTargetTransform.DOLocalRotate(new Vector3(0, 0, 0), 0.8f).SetEase(Ease.OutExpo);
        _HipTargetTransform.DOLocalRotate(new Vector3(0, 0, 0), 0.8f).SetEase(Ease.OutExpo);

        gameObject.transform.DOMove(_patientSitTransform.position, 1.0f).SetEase(Ease.OutExpo);
        gameObject.transform.DORotateQuaternion(_patientSitTransform.rotation, 1.0f).SetEase(Ease.OutExpo);

        _animator.SetBool("Walk", false); //Play Sit Animation
        _aiFeedback.ConversationCanvas.SetActive(true);
        _aiFeedback.SetConversationText("대상자의 증상 관찰 및환자 행동을 확인하세요.");
        StartCoroutine(Calm());
    }
    public void MoveToSitPosition()
    {
        if (_agent != null) _agent.enabled = false;

        float sitDuration = 2.2f;

        // 1. Exact Position aur Rotation jo aapne inspector mein dikhayi
        Vector3 targetPos = new Vector3(-1.913534f, 0.9514837f, 0.696336f);
        Vector3 targetRot = new Vector3(4.172f, -270.422f, -2.28f);

        // Body movement smoothly start karein
        gameObject.transform.DOMove(targetPos, sitDuration).SetEase(Ease.OutQuad);
        gameObject.transform.DORotateQuaternion(Quaternion.Euler(targetRot), sitDuration).SetEase(Ease.OutQuad);

        // 2. Spine aur Hip reset taake posture seedha rahe
        _spineTargetTransform.DOLocalRotate(Vector3.zero, sitDuration).SetEase(Ease.OutQuad);
        _HipTargetTransform.DOLocalRotate(Vector3.zero, sitDuration).SetEase(Ease.OutQuad);

        // 3. Mobile Target (mobarea) ko bhi uski sahi jagah move karein
        Vector3 mobPos = new Vector3(-0.292f, 0.368f, 0.241f);
        Vector3 mobRot = new Vector3(-29.886f, 188.209f, 81.757f);

        // mobarea ko rig ke relative local move karein
        _mobileTarget.DOLocalMove(mobPos, sitDuration).SetEase(Ease.OutQuad);
        _mobileTarget.DOLocalRotate(mobRot, sitDuration).SetEase(Ease.OutQuad);
        //// NavMeshAgent ko stop karna zaroori hai taake DOTween kaam kare
        //if (_agent != null) _agent.enabled = false;

        //// Duration (slow movement ke liye 2.2 seconds rakha hai)
        //float sitDuration = 2.2f;

        //// 1. Patient ki Body position aur rotation par move karein
        //// _patientSitTransform wo position hai jo aapne pehle se set ki hui hai
        //gameObject.transform.DOMove(_patientSitTransform.position, sitDuration).SetEase(Ease.OutQuad);
        //gameObject.transform.DORotateQuaternion(_patientSitTransform.rotation, sitDuration).SetEase(Ease.OutQuad);

        //// 2. Spine aur Hip ko bhi reset karein taake baki jism seedha rahe
        //_spineTargetTransform.DOLocalRotate(new Vector3(0, 0, 0), sitDuration).SetEase(Ease.OutQuad);
        //_HipTargetTransform.DOLocalRotate(new Vector3(0, 0, 0), sitDuration).SetEase(Ease.OutQuad);
    }

    public void LookAtTargetmedi()
    {
        _agent.enabled = false;
        HandleSuitCase(true);
        DisablePatientWires();
        _patientFacialController.SetExpression(ExpressionType.Confusion);

        // 1. Duration ko 2.2 seconds kar diya (Pehle 4.0 tha, is liye slow lag raha tha)
        float sitDuration = 2.2f;

        // 2. Body movement thodi taiz aur smooth
        gameObject.transform.DOMove(_patientSitTransform.position, sitDuration).SetEase(Ease.OutQuad);
        gameObject.transform.DORotateQuaternion(_patientSitTransform.rotation, sitDuration).SetEase(Ease.OutQuad);

        // 3. Spine aur Hip rotation
        _spineTargetTransform.DOLocalRotate(new Vector3(0, 0, 0), sitDuration).SetEase(Ease.OutQuad);
        _HipTargetTransform.DOLocalRotate(new Vector3(0, 0, 0), sitDuration).SetEase(Ease.OutQuad);
        // 4. Animation foran start karein magar CrossFade ke saath taiz transition dein
        // 0.3s ka transition jhatka khatam karega magar speed banaye rakhega
        _animator.CrossFade("sitting", 0.3f);
        _animator.speed = 0.85f; // Speed 0.6 se barha kar 0.85 kar di

        // 5. Gardan ko move karne ka delay bhi kam kar diya
    }

    public void LookAtTarget()
    {
        _agent.enabled = false;
        HandleSuitCase(true);
        DisablePatientWires();
        _patientFacialController.SetExpression(ExpressionType.Confusion);

        // 1. Duration ko 2.2 seconds kar diya (Pehle 4.0 tha, is liye slow lag raha tha)
        float sitDuration = 2.2f;

        // 2. Body movement thodi taiz aur smooth
        gameObject.transform.DOMove(_patientSitTransform.position, sitDuration).SetEase(Ease.OutQuad);
        gameObject.transform.DORotateQuaternion(_patientSitTransform.rotation, sitDuration).SetEase(Ease.OutQuad);

        // 3. Spine aur Hip rotation
        _spineTargetTransform.DOLocalRotate(new Vector3(0, 0, 0), sitDuration).SetEase(Ease.OutQuad);
        _HipTargetTransform.DOLocalRotate(new Vector3(0, 0, 0), sitDuration).SetEase(Ease.OutQuad);

        // 4. Animation foran start karein magar CrossFade ke saath taiz transition dein
        // 0.3s ka transition jhatka khatam karega magar speed banaye rakhega
        _animator.CrossFade("sitting", 0.3f);
        _animator.speed = 0.85f; // Speed 0.6 se barha kar 0.85 kar di

        // 5. Gardan ko move karne ka delay bhi kam kar diya
        DOVirtual.DelayedCall(1.2f, () => {
            //LookAtTwoTargets(1, 3);
            LookAtTargetByIndex(0);
            _animator.speed = 1.0f; // Normal speed par wapas
        });
    }
  public void LookAtTwoTargets(int firstIndex, int secondIndex)
{
    if (_neckAimConstraint == null) return;

    var data = _neckAimConstraint.data.sourceObjects;

    // Har target ke liye loop chalayein taake weight smooth tareeqe se change ho
    for (int i = 0; i < data.Count; i++)
    {
        float targetWeight = (i == firstIndex || i == secondIndex) ? 1f : 0f;
        
        // DOTween use kar rahe hain taake weight 1 second mein smooth change ho
        int index = i; // Closure ke liye index save karein
        DOTween.To(() => _neckAimConstraint.data.sourceObjects.GetWeight(index), 
                   x => {
                       var tempWeights = _neckAimConstraint.data.sourceObjects;
                       tempWeights.SetWeight(index, x);
                       _neckAimConstraint.data.sourceObjects = tempWeights;
                   }, 
                   targetWeight, 1.5f).SetEase(Ease.InOutSine); 
    }

    HandlePatientNeck(true);
    _idleAnim = false;
}
    public void LookAtTargetByIndex(int targetIndex)
    {
        if (_neckAimConstraint == null) return;

        var data = _neckAimConstraint.data.sourceObjects;

        for (int i = 0; i < data.Count; i++)
        {
            // Check karein ke ye wahi index hai jisay dikhana hai ya nahi
            float finalWeight = (i == targetIndex) ? 1f : 0f;

            int index = i; // Closure handle karne ke liye variable

            // DOTween ke zariye weight ko smooth transition dena
            DOTween.To(() => _neckAimConstraint.data.sourceObjects.GetWeight(index),
                       x => {
                           var tempWeights = _neckAimConstraint.data.sourceObjects;
                           tempWeights.SetWeight(index, x);
                           _neckAimConstraint.data.sourceObjects = tempWeights;
                       },
                       finalWeight, 1.2f) // 1.2 seconds mein gardan muregi
                       .SetEase(Ease.OutSine); // Shuru mein tez aur end mein thoda slow (natural)
        }

        // Rig builder ko refresh karna zaroori hai
        _rigBuilder.Build();
        HandlePatientNeck(true);
        _idleAnim = false;
    }
    private IEnumerator Calm()
    {
        yield return new WaitForSeconds(2f);

        HandlePatientNeck(true);
        LookAtTargetByIndex(0);

        _isTalking = true;
        _patientAudioSource.clip = _patientCalmClip;
        _patientAudioSource.Play();
        yield return new WaitForSeconds(_patientCalmClip.length);

        HandlePatientNeck(false);
        _isTalking = false;
        yield return new WaitForSeconds(2f);
        _aiFeedback.ConversationCanvas.SetActive(true);
        string text3 = "따님께서 회사에서 퇴근하면 저녁에 병원으로 오신다고 전화왔었어요.";
        _aiFeedback.SetConversationText(text3);
        _aiFeedback.StartListening(_aiFeedback.AiFeedbackClip03, text3);
        _aiFeedback.StartBlinking();
    }

    private void DisablePatientWires()
    {
        foreach (var wire in _patientWires)
        {
            wire.SetActive(false);
        }
    }

    public void ResetPatient()
    {
        gameObject.transform.DOMove(_patientLyingTransform.position, 1.0f).SetEase(Ease.OutExpo);
        gameObject.transform.DORotateQuaternion(_patientLyingTransform.rotation, 1.0f).SetEase(Ease.OutExpo);
        _animator.SetBool("Sit", true);
        _patientFacialController.SetExpression(ExpressionType.Sleeping);
    }

    private void HandleSuitCase(bool isAtBed)
    {
        if (isAtBed)
        {
            _bedSuitcase.SetActive(true);
            _patientSuitcase.SetActive(false);
        }
        else
        {
            _bedSuitcase.SetActive(false);
            _patientSuitcase.SetActive(true);
        }
    }

    public void HandlePatientNeck(bool isEnabled)
    {
        _rigBuilder.layers[1].active = isEnabled;
    }
}
