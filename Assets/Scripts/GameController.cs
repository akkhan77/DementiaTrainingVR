using System.Collections;
using TMPro;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController instance;
    public enum InterventionStage { Clock, Booklet, Photo, VideoCall, Music, Medicine }
    public InterventionStage currentStage = InterventionStage.Clock;
    // Stage 1-1 State Variables
    private bool handSanitized = false;
    public Animator handsnitizerAnim;
    public MeshRenderer sanitizerRenderer;
    private GameObject lastHighlightedObject;
    private Color originalColor;
    public GameObject braceletP;
    public GameObject knIPPanel;
    public GameObject quizP;
    public GameObject quizbtn;
    [Header("Stage 1-3: Concept Explanation")]
    [SerializeField] private GameObject _explanationPanel;
    [SerializeField] private AudioSource _feedbackSource;

    // 3D Models ki Outline scripts ko yahan drag karein
    [SerializeField] private Outline _femaleNurseOutline;
    [SerializeField] private Outline _maleNurseOutline;
    [SerializeField] private GameObject _femaleNurseblinker;
    [SerializeField] private GameObject _maleNurseblinker;

    public AudioClip NurseFemaleClip01;
    public AudioClip NurseMaleClip02;

    private Coroutine _nurseBlinkCoroutine;
    private bool _isNurseBlinking = false;
    [Header("Stage 1-4 Settings")]
    public Outline clockOutline;
    public Outline calendarOutline;
    public AudioClip ai_feedback04;
    private bool isStage1_4Ready = false;

    [Header("Stage 1-5: Hospital Booklet")]
    public Outline bookletOutline;
    public AudioClip ai_feedback05; // ai_feedback05.mp3
    private bool isBookletReady = false;

    [Header("New Intervention Outlines")]
    public Outline photoOutline;
    public Outline videoCallOutline;
    public Outline musicOutline;
    public Outline medicineOutline;

    public GameObject photoButton;
    public GameObject videoCallButton;
    public GameObject musicButton;
    public GameObject medicineButton;
    [Header("New AI Clips")]
    public AudioClip ai_feedback06; // Family Photo
    public AudioClip ai_feedback07; // Video Call
    public AudioClip ai_feedback08; // Favorite Song
    public AudioClip ai_feedback09; // Medication
    private bool isItemReady = false; // Click control karne ke liye
    [Header("Patient Final Response")]
    public AudioClip patientClip15; // "그럼 지금은 사진보면서 기다려봐요." (Let's wait while looking at photo)
    public AudioClip patientClip16;
    public AudioSource PatientAudio;

    [Header("End Scenarios & Guide")]
    public GameObject guideNurseCharacter; // Inspector mein Guide Nurse ka 3D model/prefab yahan drag karein
    public GameObject episodeSelectionPanel;
    public AudioClip trainingEndedClip12;
    public AudioClip debriefingClip11;
    public AudioClip aiSummaryClip10;
    [Header("Settings")]
    public float updateInterval = 1.0f; // How often numbers change (seconds)
    private float timer;
    [Header("Text References")]
    public TextMeshProUGUI hrText;   // Heart Rate
    public TextMeshProUGUI bpText;   // Blood Pressure
    public TextMeshProUGUI rrText;   // Resp Rate
    public TextMeshProUGUI btText;   // Temperature
    public TextMeshProUGUI spo2Text; // Oxygen
    void Awake()
    {
        if (photoButton) photoButton.SetActive(false);
        if (videoCallButton) videoCallButton.SetActive(false);
        if (musicButton) musicButton.SetActive(false);
        if (medicineButton) medicineButton.SetActive(false);
    }
    void Start()
    {
        instance = this;
        // AIFeedbackController ke event ko listen karein
        //if (AIFeedbackController.instance != null)
        //{
        //    AIFeedbackController.instance.OnAudioFinished += HandleAudioFinished;
        //}
    }
    private void Update()
    {
        //AudioSource[] sources = FindObjectsOfType<AudioSource>();

        //foreach (AudioSource src in sources)
        //{
        //    if (src.isPlaying && src.clip != null)
        //    {
        //        Debug.Log($"Playing Sound: {src.clip.name} | Object: {src.gameObject.name}");
        //    }
        timer += Time.deltaTime;

        if (timer >= updateInterval)
        {
            UpdateAllVitals();
            timer = 0; // Reset timer
        }
    }

    void UpdateAllVitals()
    {
        // 1. Heart Rate: Random wiggle between 108 and 112
        int hr = Random.Range(108, 113);
        hrText.text = $". HR <color=red>{hr}</color>회/분";

        // 2. Blood Pressure: Keeps 120/80 but wiggles the first number slightly
        int systolic = Random.Range(118, 123);
        bpText.text = $". BP {systolic}/80mmHg";

        // 3. Respiration Rate: Random wiggle between 18 and 22
        int rr = Random.Range(18, 23);
        rrText.text = $". RR {rr}회/분";

        // 4. Body Temperature: Random wiggle between 36.5 and 36.8
        float bt = Random.Range(36.5f, 36.9f);
        btText.text = $". BT {bt:F1}°C"; // :F1 ensures one decimal place

        // 5. SpO2: Stays mostly at 99%, occasionally drops to 98%
        int spo2 = (Random.value > 0.8f) ? 98 : 99;
        spo2Text.text = $". SpO2: {spo2}%";
    }
    /// <summary>
    /// ///////
    /// </summary>
    /// 

    // Purane HandleAudioFinished ko is se replace karein taake buttons ek ke baad ek on hon
    private void HandleAudioFinished()
    {
        if (AIFeedbackController.instance != null)
        {
            AIFeedbackController.instance.ConversationCanvas.SetActive(false);
        }
        if (currentStage == InterventionStage.Medicine)
        {
            StartCoroutine(TriggerPatientFinalResponse());
            return;
        }
        StartCoroutine(ActivateNextStepWithDelay(1.5f)); // 1.5 seconds ka gap
    }
    private IEnumerator TriggerPatientFinalResponse()
    {
        // 1. Medicine Button band karein
        if (medicineButton != null) medicineButton.SetActive(false);

        yield return new WaitForSeconds(1.0f);

        // 2. Clip 15 Play karein
        AIFeedbackController.instance.ConversationCanvas.SetActive(true);
        AIFeedbackController.instance.SetConversationText("그럼 지금은 사진보면서 기다려봐요.");

        if (PatientAudio != null && patientClip15 != null)
        {
            PatientAudio.clip = patientClip15;
            PatientAudio.Play();
            // Wait jab tak Clip 15 khatam na ho
            yield return new WaitWhile(() => PatientAudio.isPlaying);
        }

        yield return new WaitForSeconds(0.5f);

        // 3. Clip 16 Play karein
        AIFeedbackController.instance.SetConversationText("딸이 시간되어도 안오면 전화해줘요.");

        if (PatientAudio != null && patientClip16 != null)
        {
            PatientAudio.clip = patientClip16;
            PatientAudio.Play();

            // --- YE SAB SE ZAROORI LINE HAI ---
            // Jab tak Clip 16 chal rahi hai, ye coroutine yahan ruka rahay ga
            yield return new WaitWhile(() => PatientAudio.isPlaying);
        }

        // 4. CLIP 16 KHATAM HONE KE BAAD YE CHALAY GA:
        EpisodeManager.isGameComplete = true; // Bool set karein taake next click par Nurse aaye

        // Dialogue box band karein
        if (AIFeedbackController.instance != null)
        {
            AIFeedbackController.instance.ConversationCanvas.SetActive(false);
            AIFeedbackController.instance.OnAudioFinished -= HandleAudioFinished;
        }

        // Ab Episode Panel dikhayein
        if (episodeSelectionPanel != null)
        {
            episodeSelectionPanel.SetActive(true);
            Debug.Log("Clip 16 Finished: Opening Episode Panel now.");
        }
    }
    private IEnumerator ActivateNextStepWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        switch (currentStage)
        {
            case InterventionStage.Clock:
                currentStage = InterventionStage.Booklet;
                StartStage1_5Blinking(); // Model blink shuru
                break;

            case InterventionStage.Booklet:
                currentStage = InterventionStage.Photo;
                if (photoButton) photoButton.SetActive(true); // Family Photo button ON
                break;

            case InterventionStage.Photo:
                currentStage = InterventionStage.VideoCall;
                if (photoButton) photoButton.SetActive(false); // Purana band
                if (videoCallButton) videoCallButton.SetActive(true); // Naya ON
                break;

            case InterventionStage.VideoCall:
                currentStage = InterventionStage.Music;
                if (videoCallButton) videoCallButton.SetActive(false);
                if (musicButton) musicButton.SetActive(true);
                break;

            case InterventionStage.Music:
                currentStage = InterventionStage.Medicine;
                if (musicButton) musicButton.SetActive(false);
                if (medicineButton) medicineButton.SetActive(true);
                break;
        }
    }
    // Naya helper function buttons manage karne ke liye
    private void EnableNextButton(Outline nextOutline)
    {
        isItemReady = true;
        // Purane sare outlines ko off kar dein (Safety ke liye)
        StopStage1_4Visuals();
        if (bookletOutline) bookletOutline.OutlineWidth = 0f;

        // Naye wale ko blink karwayein
        StartCoroutine(GenericBlinkRoutine(nextOutline));
    }
    public void OnPhotoClick()
    {
        if (currentStage == InterventionStage.Photo)
        {
            AIFeedbackController.instance.ConversationCanvas.SetActive(true);
            string txt = "서영호님, 따님을 기다리면서 가족사진을 볼까요?";
            AIFeedbackController.instance.SetConversationText(txt);
            AIFeedbackController.instance.StartBlinking();

            AIFeedbackController.instance.StartListening(ai_feedback06, txt);
        }
    }

    public void OnVideoCallClick()
    {
        if (currentStage == InterventionStage.VideoCall)
        {
            AIFeedbackController.instance.ConversationCanvas.SetActive(true);
            string txt = "따님과 영상통화 연결해 드릴까요?";
            AIFeedbackController.instance.SetConversationText(txt);
            AIFeedbackController.instance.StartBlinking();

            AIFeedbackController.instance.StartListening(ai_feedback07, txt);
        }
    }

    public void OnMusicClick()
    {
        if (currentStage == InterventionStage.Music)
        {
            AIFeedbackController.instance.ConversationCanvas.SetActive(true);
            string txt = "좋아하는 노래를 들으실까요?";
            AIFeedbackController.instance.SetConversationText(txt);
            AIFeedbackController.instance.StartBlinking();

            AIFeedbackController.instance.StartListening(ai_feedback08, txt);
        }
    }

    public void OnMedicineClick()
    {
        if (currentStage == InterventionStage.Medicine)
        {
            AIFeedbackController.instance.ConversationCanvas.SetActive(true);
            string txt = "필요시 약물적 중재를 적용할 수 있습니다.";
            AIFeedbackController.instance.SetConversationText(txt);
            AIFeedbackController.instance.StartBlinking();

            AIFeedbackController.instance.StartListening(ai_feedback09, txt);
        }
    }
    private IEnumerator GenericBlinkRoutine(Outline target)
    {
        while (isItemReady)
        {
            float width = (Mathf.Sin(Time.time * 4f) + 1f) * 2.5f;
            if (target) target.OutlineWidth = width;
            yield return null;
        }
    }

    public void StartStage1_5Blinking()
    {
        isBookletReady = true;
        StartCoroutine(BookletBlinkRoutine()); // Booklet blink karna shuru karegi
    }

    private IEnumerator BookletBlinkRoutine()
    {
        while (isBookletReady)
        {
            float width = (Mathf.Sin(Time.time * 4f) + 1f) * 2.5f;
            if (bookletOutline) bookletOutline.OutlineWidth = width;
            yield return null;
        }
    }
    public void OnBookletClick()
    {
        if (isBookletReady)
        {
            isBookletReady = false;
            if (bookletOutline) bookletOutline.OutlineWidth = 0f;
            AIFeedbackController.instance.ConversationCanvas.SetActive(true);
            string stage5Text = "네, 맞아요. 서영호님은 지금 동산병원에 입원해 계세요. 입원하셔서 치료를 잘 받고 계세요. 서영호님이 잘 지내실 수 있도록 옆에서 도와드릴게요.";
            AIFeedbackController.instance.SetConversationText(stage5Text);
            AIFeedbackController.instance.StartBlinking();

            AIFeedbackController.instance.StartListening(ai_feedback05, stage5Text);
        }
    }

    //private void HandleAudioFinished()
    //{
    //    // Har audio khatam hone par agla stage set karein
    //    switch (currentStage)
    //    {
    //        case InterventionStage.Clock:
    //            currentStage = InterventionStage.Booklet;
    //            StartStage1_5Blinking(); // Booklet (Jo aapne pehle likha tha)
    //            break;

    //        case InterventionStage.Booklet:
    //            currentStage = InterventionStage.Photo;
    //            StartBlinkEffect(photoOutline); // Photo blinking shuru
    //            break;

    //        case InterventionStage.Photo:
    //            currentStage = InterventionStage.VideoCall;
    //            StartBlinkEffect(videoCallOutline); // Video Call blinking
    //            break;

    //        case InterventionStage.VideoCall:
    //            currentStage = InterventionStage.Music;
    //            StartBlinkEffect(musicOutline); // Music blinking
    //            break;

    //        case InterventionStage.Music:
    //            currentStage = InterventionStage.Medicine;
    //            StartBlinkEffect(medicineOutline); // Medicine blinking
    //            break;
    //    }
    //}
    private void StartBlinkEffect(Outline targetOutline)
    {
        isItemReady = true;
        StartCoroutine(GenericBlinkRoutine(targetOutline));
    }

   
    public void Quiz_Btn()
    {
        knIPPanel.SetActive(false);
        quizbtn.SetActive(false);
        quizP.SetActive(true);

    }



    public void OnClockOrCalendarClick()
    {
        if (isStage1_4Ready)
        {
            isStage1_4Ready = false; // Dobara click na ho
            StopStage1_4Visuals(); // Blinking rok dein
            AIFeedbackController.instance.ConversationCanvas.SetActive(true);
            // Dialogue aur Mic on karein
            string stage1_4Text = "서영호님 지금은 오후 6시에요. 따님 오실 때까지 제가 곁에 있을께요.";
            AIFeedbackController.instance.SetConversationText(stage1_4Text);
            AIFeedbackController.instance.StartBlinking();
            if (AIFeedbackController.instance != null)
            {
                AIFeedbackController.instance.StartListening(ai_feedback04, stage1_4Text);
            }
        }
    }
    private IEnumerator ClockCalendarBlinkRoutine()
    {
        while (isStage1_4Ready)
        {
            float width = (Mathf.Sin(Time.time * 4f) + 1f) * 2.5f;
            if (clockOutline) clockOutline.OutlineWidth = width;
            if (calendarOutline) calendarOutline.OutlineWidth = width;
            yield return null;
        }
    }
    private IEnumerator PlayEndSequence()
    {
        yield return new WaitForSeconds(0.5f);

        // 1. Clip 11: Nurse Question (Debriefing)
        AIFeedbackController.instance.ConversationCanvas.SetActive(true);
        AIFeedbackController.instance.SetConversationText("네 가지 상황 중 가장 어려웠던 점 하나만 말해주세요.");
        AIFeedbackController.instance.StartBlinking();
        if (_feedbackSource != null && debriefingClip11 != null)
        {
            _feedbackSource.clip = debriefingClip11;
            _feedbackSource.Play();
            yield return new WaitWhile(() => _feedbackSource.isPlaying);
        }

        // 2. User Response & AI Summary (Clip 10)
        // Hum AIFeedbackController ko bolenge ke Clip 10 chalao
        AIFeedbackController.instance.StartListening(aiSummaryClip10, "Listening to your response...");

        // --- ZAROORI FIX: Clip 12 ko tab tak rokein jab tak AI Summary (Clip 10) khatam na ho jaye ---
        // Hum AIFeedbackController ke AudioSource ko check karenge
        AudioSource aiSource = AIFeedbackController.instance.GetComponent<AudioSource>();

        // Pehle wait karein ke AI Summary bajna shuru ho (thira gap)
        yield return new WaitForSeconds(2.0f);

        // Ab wait karein jab tak AI Summary (Clip 10) baj rahi hai
        yield return new WaitWhile(() => aiSource.isPlaying);

        // 3. Clip 12: Training Ended (Ab ye tabhi chalega jab AI Summary khatam ho chuki hogi)
        if (trainingEndedClip12 != null)
        {
            AIFeedbackController.instance.SetConversationText("훈련이 종료되었습니다. 수고하셨습니다.");
            AIFeedbackController.instance.StartBlinking();

            _feedbackSource.clip = trainingEndedClip12;
            _feedbackSource.Play();
            yield return new WaitWhile(() => _feedbackSource.isPlaying);
        }

        Debug.Log("Full Sequence Completed without Audio Overlap.");
    }
    
    public void EndGame()
    {
        StartCoroutine(PlayEndSequence());

    }

    public void StopStage1_4Visuals()
    {
        if (clockOutline) clockOutline.OutlineWidth = 0f;
        if (calendarOutline) calendarOutline.OutlineWidth = 0f;
    }

    //// Update is called once per frame
    //void Update()
    //{
    //    Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    //    RaycastHit hit;

    //    if (Physics.Raycast(ray, out hit, 5f))
    //    {
    //        GameObject currentObject = hit.collider.gameObject;

    //        // Agar naya object hit hua hai
    //        if (currentObject != lastHighlightedObject)
    //        {
    //            ResetHighlight(); // Purane object ka rang wapis sahi karein

    //            lastHighlightedObject = currentObject;

    //            if (currentObject.GetComponent<Renderer>() != null)
    //            {
    //                originalColor = currentObject.GetComponent<Renderer>().material.color;
    //                currentObject.GetComponent<Renderer>().material.color = Color.yellow; // Highlight color
    //            }
    //        }

    //        Debug.Log("Hit: " + hit.collider.name + " | Layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));
    //    }
    //    else
    //    {
    //        ResetHighlight();
    //    }
    //}

    //void ResetHighlight()
    //{
    //    if (lastHighlightedObject != null)
    //    {
    //        lastHighlightedObject.GetComponent<Renderer>().material.color = originalColor;
    //        lastHighlightedObject = null;
    //    }
    //}

    public void OnHandSanitizerClick()
    {
        if (!handSanitized)
        {
            StartCoroutine(HandleSanitizationProcess());
        }
    }

    private IEnumerator HandleSanitizationProcess()
    {
        handSanitized = true;

        if (EpisodeOneFlowController.instance != null)
        {
            // 1. Sanitizer wala blinker (0) band karein
            EpisodeOneFlowController.instance.SetBlinker(0, false);
        }

        if (handsnitizerAnim != null)
        {
            handsnitizerAnim.enabled = true;
            yield return new WaitForSeconds(2.0f); // Animation ka wait
        }

        if (EpisodeOneFlowController.instance != null)
        {
            // 2. Player ko move karein
            EpisodeOneFlowController.instance.MovePlayerToNewPosition();

            // 3. State change karein
            EpisodeOneFlowController.instance.ChangeState(Episode1State.BraceletCheck, 0.1f);

            // 4. AB BRACELET WALA BLINKER (1) ON KAREIN
            EpisodeOneFlowController.instance.SetBlinker(1, true);
        }

        if (sanitizerRenderer != null) sanitizerRenderer.enabled = false;
    }

   
    public void Bracelet()
    {
        // Bracelet ka panel on karein
        braceletP.SetActive(true);

        // Timer aur Movement shuru karne ke liye Coroutine call karein
        StartCoroutine(HandleBraceletAndMoveBack());
    }
    private IEnumerator HandleBraceletAndMoveBack()
    {
        // 1. Pehle 4 seconds tak panel dikhayein
        yield return new WaitForSeconds(4f);

        // 2. Panel band karein
        braceletP.SetActive(false);

        if (EpisodeOneFlowController.instance != null)
        {
            // 3. Bracelet ka blinker (1) band karein
            EpisodeOneFlowController.instance.SetBlinker(1, false);

            // 4. Player ko wapis purani position par bhejein
            // Hum wahi function use karenge jo player ko move karta hai
            // Agar aapke paas start position ka variable hai to wo use karein
            EpisodeOneFlowController.instance.MovePlayerToStartPosition();

            // 5. Agli state (Introduction) par jayein
            EpisodeOneFlowController.instance.ChangeState(Episode1State.Introduction);
        }
    }


    public void ShowExplanationPanel()
    {
        _explanationPanel.SetActive(true);
        // Sequence shuru karein
        StartCoroutine(NurseExplanationSequence());
    }

    private IEnumerator NurseExplanationSequence()
    {
        // 1. Pehle Female Nurse ki bari
        if (_femaleNurseOutline != null) {
            _femaleNurseblinker.GetComponent<OutlineBlinker>().enabled = true;
            _maleNurseblinker.GetComponent<OutlineBlinker>().enabled = false;

            _femaleNurseOutline.OutlineWidth = 6f;
        } 
        if (_maleNurseOutline != null) _maleNurseOutline.OutlineWidth = 0f;

        _feedbackSource.clip = NurseFemaleClip01;
        _feedbackSource.Play();

        // Intezar karein jab tak female ki audio khatam na ho jaye
        yield return new WaitWhile(() => _feedbackSource.isPlaying);
        yield return new WaitForSeconds(0.5f); // Thora sa gap

        // 2. Ab Male Nurse ki bari
        if (_femaleNurseOutline != null) _femaleNurseOutline.OutlineWidth = 0f;
        if (_maleNurseOutline != null) { _maleNurseOutline.OutlineWidth = 5f;
            _femaleNurseblinker.GetComponent<OutlineBlinker>().enabled = false;
            _maleNurseblinker.GetComponent<OutlineBlinker>().enabled = true;

        }

        _feedbackSource.clip = NurseMaleClip02;
        _feedbackSource.Play();

        // Intezar karein jab tak male ki audio khatam na ho jaye
        yield return new WaitWhile(() => _feedbackSource.isPlaying);

        // 3. Dono khatam hone par Outline band kar dein ya dono on rakhein (aapki marzi)
        if (_maleNurseOutline != null) _maleNurseOutline.OutlineWidth = 0f;

        Debug.Log("Dono explanations mukammal ho gayin.");
    }

    public void OnSkipClick()
    {
        StopAllCoroutines(); // Sequence ko rok dein
        _feedbackSource.Stop(); // Audio band karein
        if (_femaleNurseOutline)
        {
            _femaleNurseOutline.OutlineWidth = 0f;
        }
        if (_maleNurseOutline)
        {
            _maleNurseOutline.OutlineWidth = 0f;
        }
        _femaleNurseblinker.GetComponent<OutlineBlinker>().enabled = false;
        _maleNurseblinker.GetComponent<OutlineBlinker>().enabled = false;

        _explanationPanel.SetActive(false);
        if (AIFeedbackController.instance != null)
        {
            // Pehle purana koi listener ho to clear karein (safety)
            AIFeedbackController.instance.OnAudioFinished -= HandleAudioFinished;
            // Phir naya subscribe karein
            AIFeedbackController.instance.OnAudioFinished += HandleAudioFinished;
        }

        // Pehla stage (Clock/Calendar) shuru karein
        currentStage = InterventionStage.Clock;
        StartStage1_4Blinking();
    }
    private void StartStage1_4Blinking()
    {
        isStage1_4Ready = true; // Ab click detect ho sakega
        StartCoroutine(ClockCalendarBlinkRoutine()); // Blinking shuru
    }

}


