using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.Animations.Rigging;

public class PatientController : MonoBehaviour
{
    [SerializeField] private Transform _XrRigTransform;
    [SerializeField] private Animator _animator;
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
    private bool _isTalking = false;
    private bool _blink = false;
    private bool _idleAnim = false;

    private int _mouthBlendShapeIndex;
    private int _eyesBlendShapeIndex;
    private float _blinkTimer;
    private float _blinkValue;
    private bool _isBlinking;
    private bool _hasArrived;
    private Transform _currentTarget;

    public PatientFacialController FacialController => _patientFacialController;

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

        if (_idleAnim)
        {
            HandleReachedDestination();
        }
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
        _spineTargetTransform.DOLocalRotate(new Vector3(70, -22, 0), 1f).SetEase(Ease.OutExpo);
        _HipTargetTransform.DOLocalRotate(new Vector3(0, -3, 0), 1f).SetEase(Ease.OutExpo);

        _patientFacialController.SetExpression(ExpressionType.Aggressive);
        _aiFeedback.SetConversationText("대상자의 증상 관찰 및 환자행동을확인하세요.");
        StartCoroutine(Confusion());
    }

    private IEnumerator Confusion()
    {
        yield return new WaitForSeconds(2f);
        gameObject.transform.DORotateQuaternion(_patientSitTransform.rotation, 1.0f).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(0.18f);
        Debug.Log("11111");
        _patientFacialController.SetExpression(ExpressionType.Confusion);
        _spineTargetTransform.localRotation = Quaternion.Euler(0, 0, 0);
        _HipTargetTransform.localRotation = Quaternion.Euler(0, 0, 0);

        _agent.enabled = true;
        DisablePatientWires();
        _isWandering = true;
        _animator.SetBool("Walk", true);

        HandleSuitCase(false);
        yield return new WaitForSeconds(5f);
        Debug.Log("22222");

        _idleAnim = true;
        GoToPosition(_XrRigTransform);
        yield return new WaitForSeconds(3f);
        Debug.Log("33333");

        HandlePatientNeck(true);
        _isTalking = true;
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
        yield return new WaitForSeconds(8f);

        HandlePatientNeck(false);
        _animator.SetBool("Idle", false);
        _idleAnim = false;
        GoToPosition(_bedPoint);
    }

    public void PatientSitDown()
    {
        _agent.enabled = false;
        HandleSuitCase(true);
        DisablePatientWires();

        _spineTargetTransform.DOLocalRotate(new Vector3(0, 0, 0), 0.8f).SetEase(Ease.OutExpo);
        _HipTargetTransform.DOLocalRotate(new Vector3(0, 0, 0), 0.8f).SetEase(Ease.OutExpo);

        gameObject.transform.DOMove(_patientSitTransform.position, 1.0f).SetEase(Ease.OutExpo);
        gameObject.transform.DORotateQuaternion(_patientSitTransform.rotation, 1.0f).SetEase(Ease.OutExpo);

        _animator.SetBool("Walk", false); //Play Sit Animation
        _aiFeedback.SetConversationText("대상자의 증상 관찰 및환자 행동을 확인하세요.");
        StartCoroutine(Calm());
    }

    private IEnumerator Calm()
    {
        yield return new WaitForSeconds(2f);

        HandlePatientNeck(true);
        _isTalking = true;
        _patientAudioSource.clip = _patientCalmClip;
        _patientAudioSource.Play();
        yield return new WaitForSeconds(_patientCalmClip.length);

        HandlePatientNeck(false);
        _isTalking = false;
        yield return new WaitForSeconds(2f);
        _aiFeedback.SetConversationText("따님께서회사에서 퇴근하면 저녁에 병원으로 오신다고 전화왔었어요.");
        _aiFeedback.StartListening(_aiFeedback.AiFeedbackClip03);
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

    private void HandlePatientNeck(bool isEnabled)
    {
        _rigBuilder.layers[1].active = isEnabled;
    }
}
