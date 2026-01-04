using UnityEngine;

public class StageManager : MonoBehaviour
{
    [SerializeField] private EpisodeOneFlowController _episode1FlowController;
    [SerializeField] private AIFeedbackController _aiFeedbackController;
    [SerializeField] private PatientController _patientController;

    private int _HandSanitizerIndex = 0;

    void OnEnable()
    {
        _aiFeedbackController.OnConversationEnded += ManageIntroductionStage;
    }

    public void ManageHandSanitizerStage()
    {
        _HandSanitizerIndex++;
        if (_HandSanitizerIndex == 2)
        {
            _episode1FlowController.ChangeState(Episode1State.Introduction);
            _patientController.FacialController.SetExpression(ExpressionType.Neutral);
            Debug.Log("First time picking up hand sanitizer.");
        }
        else
        {
            Debug.Log("Picked up hand sanitizer again. Count: " + _HandSanitizerIndex);
        }
    }

    private void ManageIntroductionStage()
    {
        if (_episode1FlowController.CurrentState == Episode1State.Introduction)
        {
            _episode1FlowController.ChangeState(Episode1State.PatientAggression, 1f);
        }
        else if (_episode1FlowController.CurrentState == Episode1State.PatientAggression)
        {
            _episode1FlowController.ChangeState(Episode1State.PatientCalm, 1f);
        }
        else if (_episode1FlowController.CurrentState == Episode1State.PatientCalm)
        {
            _episode1FlowController.ChangeState(Episode1State.Assessment, 1f);
        }
    }

    void OnDisable()
    {
        _aiFeedbackController.OnConversationEnded -= ManageIntroductionStage;
    }
}