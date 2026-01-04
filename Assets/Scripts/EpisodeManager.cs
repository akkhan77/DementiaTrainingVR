using UnityEngine;
using UnityEngine.UI;

public class EpisodeManager : MonoBehaviour
{
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
        _episodeOneBtn.onClick.AddListener(() => EpisodeOne());
        _episodeTwoBtn.onClick.AddListener(() => EpisodeTwo());
        _episodeThreeBtn.onClick.AddListener(() => EpisodeThree());
        _episodeFourBtn.onClick.AddListener(() => EpisodeFour());
        _quitBtn.onClick.AddListener(() => QuitApplication());
    }

    private void EpisodeOne()
    {
        _player.transform.position = _playerPositionEpisodeOne;
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
