using System.Collections;
using UnityEngine;

public enum Episode1State
{
    HandSanitizer,
    BraceletCheck,
    Introduction,
    PatientAggression,
    PatientCalm,
    Assessment,
    End
}

public class EpisodeOneFlowController : MonoBehaviour
{
    public static EpisodeOneFlowController instance;
    [Header("State")]
    [SerializeField] private Episode1State _currentState;
    public GameObject assesstmentEnable;
    public GameObject SpeakPanel;

    [Header("References")]
    [SerializeField] private EpisodeManager _episodeManager;
    [SerializeField] private AIFeedbackController _aiFeedback;
    [SerializeField] private PatientController _patientController;
    [SerializeField] private GameObject _episodeOneAssessmentPanel;
    [SerializeField] private Vector3 _playerEpisodeOneAssessmentPosition = new();
    [SerializeField] private Vector3 _playerPositionEpisodeOne = new ();
    [SerializeField] private Vector3 startPosition = new ();

    [SerializeField] private OutlineBlinker[] _outlineBlinkers;
    public GameObject[] messages;

    public Episode1State CurrentState => _currentState;

    public void ChangeState(Episode1State nextState, float delay = 2f)
    {
        _currentState = nextState;
        StartCoroutine(State(delay));
    }
  
    private IEnumerator State(float delay)
    {
        yield return new WaitForSeconds(delay);
        GoToState();
    }
    private void Start()
    {
        instance = this;
    }
    private void GoToState()
    {
        switch (_currentState)
        {
            case Episode1State.HandSanitizer:
                HandSanitizer();
                Debug.Log("Episode State: " + _currentState);
                break;
            case Episode1State.BraceletCheck:
                MovePlayerToNewPosition();
                // Yahan wo logic aaye ga jab player bracelet dekhta hai
                Debug.Log("Ab Mareez ka bracelet check karein.");
                break;
            case Episode1State.Introduction:
                Introduction();
                Debug.Log("Episode State: " + _currentState);
                break;

            case Episode1State.PatientAggression:
                PatientAggression();
                Debug.Log("Episode State: " + _currentState);
                break;

            case Episode1State.PatientCalm:
                PatientCalm();
                Debug.Log("Episode State: " + _currentState);
                break;

            case Episode1State.Assessment:
                Assessment();
                Debug.Log("Episode State: " + _currentState);
                break;

            case Episode1State.End:
                Debug.Log("Episode State: " + _currentState);
                break;

            default:
                Debug.LogError("Unhandled Episode State: " + _currentState);
                break;
        }
    }

    private void HandSanitizer()
    {
        // Sab ko pehle off kar dein taake koi galti na ho
        StopAllBlinkers();

        // Sirf index 0 (Sanitizer) ko on karein
        if (_outlineBlinkers.Length > 0)
        {
            _outlineBlinkers[0].enabled = true;
        }

        // Messages ko show karne ke liye
     
    }

    // Ye naya function add karein jo kisi bhi specific blinker ko control kare
    public void SetBlinker(int index, bool status)
    {
        if (index >= 0 && index < _outlineBlinkers.Length)
        {
            _outlineBlinkers[index].enabled = status;
        }
    }

    // Sab blinkers ko band karne ke liye
    public void StopAllBlinkers()
    {
        foreach (var blinker in _outlineBlinkers)
        {
            if (blinker != null) blinker.enabled = false;
        }
    }

    public void MovePlayerToNewPosition()
    {
        // Player ko nayi position par set karein
        _episodeManager.Player.transform.position = _playerPositionEpisodeOne;
 
    }

    public void MovePlayerToStartPosition()
    {
        // Player ko nayi position par set karein
        _episodeManager.Player.transform.position = startPosition;
 
    }
    public void StopOutlineBlinkers()
    {
        foreach (var blinker in _outlineBlinkers)
        {
            blinker.enabled = false;
        }
    }
    private void Introduction()
    {
        Debug.Log("san lksa0");
        _aiFeedback.ConversationCanvas.SetActive(true);
        string introText = "안녕하세요. 담당 간호사 OOO입니다. 성함이 어떻게 되세요?";
        _aiFeedback.SetConversationText(introText);

        _aiFeedback.StartListening(_aiFeedback.AiFeedbackClip01, introText);
        _aiFeedback.StartBlinking();
    }

    private void PatientAggression()
    {
        _patientController.Aggressive();
    }

    private void PatientCalm()
    {
        _patientController.PatientSitDown();
    }

    private void Assessment()
    {
        _patientController.ResetPatient();
        SpeakPanel.SetActive(false);
        assesstmentEnable.GetComponent<Assesstment>().enabled = true;
        _episodeOneAssessmentPanel.SetActive(true);
        _episodeManager.Player.transform.position = _playerEpisodeOneAssessmentPosition;
    }




}
