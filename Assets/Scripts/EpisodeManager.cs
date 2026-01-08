using UnityEngine;
using UnityEngine.UI;

public class EpisodeManager : MonoBehaviour
{
    public static bool isGameComplete = false;
    public GameObject learning1;
    [Header("UI Buttons")]
    [SerializeField] private GameObject _episodeMenuPanel;
    [SerializeField] private Button _episodeOneBtn;
    [SerializeField] private Button _episodeTwoBtn;
    [SerializeField] private Button _episodeThreeBtn;
    [SerializeField] private Button _episodeFourBtn;
    [SerializeField] private Button _quitBtn;

    [Header("Episodes")]
    [SerializeField] private EpisodeOneFlowController _episodeOne;
    [SerializeField] private GameObject _episodeTwo;
    [SerializeField] private GameObject _episodeThree;
    [SerializeField] private GameObject _episodeFour;

    [Header("Player")]
    [SerializeField] private GameObject _player;
    [SerializeField] private Vector3 _playerPositionEpisodeOne = new();

    public GameObject Player => _player;
    void Start()
    {
        isGameComplete = false;
        _episodeOneBtn.onClick.AddListener(() => EpisodeOne());
        _episodeTwoBtn.onClick.AddListener(() => EpisodeTwo());
        _episodeThreeBtn.onClick.AddListener(() => EpisodeThree());
        _episodeFourBtn.onClick.AddListener(() => EpisodeFour());
        _quitBtn.onClick.AddListener(() => QuitApplication());
    }

    private void EpisodeOne()
    {
        if (isGameComplete)
        {
            // AGAR PORA KHAIL LIYA HAI: To Nurse aur Clips (11, 12) chalao
            if (GameController.instance.episodeSelectionPanel != null) GameController.instance.episodeSelectionPanel.SetActive(false);
            if (GameController.instance.guideNurseCharacter != null) GameController.instance.guideNurseCharacter.SetActive(true);

        GameController.instance.EndGame();
        }
        else
        {
            GameController.instance.learning_Panel.SetActive(true);
            learning1.SetActive(true);
            GameController.instance.episodeSelectionPanel.SetActive(false);
               

        }
    }

    public void HandWork()
    {
        _player.transform.position = _playerPositionEpisodeOne;
        GameController.instance.learning_Panel.SetActive(false);

        _episodeMenuPanel.SetActive(false);
        _episodeOne.ChangeState(Episode1State.HandSanitizer, 1f);
    }

    private void EpisodeTwo()
    {

    }

    private void EpisodeThree()
    {

    }

    private void EpisodeFour()
    {

    }

    private void QuitApplication()
    {
        Application.Quit();
    }
}
