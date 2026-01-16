using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;

public class PreBriefing : MonoBehaviour
{
    [Header("Slides")]
    [SerializeField] private GameObject[] _slides; // Size = 8
    [SerializeField] private float[] _slideDelays; // Size = 8 (0 = manual)

    [Header("Audio")]
    [SerializeField] private AudioClip[] _instructionClips; // Size = 8 (0,1 empty)
    [SerializeField] private AudioSource _instructionAudioSource;
    [SerializeField] private AudioSource _buttonAudioSource;

    [Header("UI Buttons")]
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _skipButton;

    [Header("Panels")]
    [SerializeField] private GameObject _preBriefingPanel;
    [SerializeField] private GameObject _mainMenuPanel;

    [Header("Guide Character")]
    [SerializeField] private GameObject _guideCharacter;
    [SerializeField] private Vector3[] _guidePositions;  // Size = 8 (used from index 2)

    [Header("Slide 3 - Multiple Instructions")]
    [SerializeField] private RectTransform[] _slide3InstructionRects; // Size = 5
    [SerializeField] private TMP_Text[] _slide3InstructionTexts; // Size = 5
    [SerializeField] private AudioClip[] _slide3InstructionClips; // Size = 5
    [SerializeField] private Color _slide3HighlightColor = Color.yellow;
    [SerializeField] private Color _slide3NormalColor = Color.white;
    [Header("Events")]
    public UnityEvent<int> OnSlideOpened;

    private const int SLIDE_3_INDEX = 2;
    private int _currentSlideIndex;
    private Coroutine _slideRoutine;

    private void Start()
    {
        _nextButton.onClick.AddListener(OnNextPressed);
        _skipButton.onClick.AddListener(OnSkipPressed);

        Invoke(nameof(Initialize), 1f);
    }

    private void Initialize()
    {
        _currentSlideIndex = 0;

        for (int i = 0; i < _slides.Length; i++)
            _slides[i].SetActive(false);

        _guideCharacter.SetActive(false);
        _nextButton.gameObject.SetActive(false);
        _skipButton.gameObject.SetActive(false);

        _mainMenuPanel.SetActive(false);
        _preBriefingPanel.SetActive(true);

        ShowSlide(0);
    }

    private void ShowSlide(int index)
    {
        if (_slideRoutine != null)
            StopCoroutine(_slideRoutine);

        _currentSlideIndex = index;

        for (int i = 0; i < _slides.Length; i++)
            _slides[i].SetActive(i == index);

        HandleButtons();
        HandleGuideCharacter();

        OnSlideOpened?.Invoke(index); // PER-SLIDE EVENT

        if (index == SLIDE_3_INDEX)
        {
            StartCoroutine(PlaySlide3Instructions());
        }
        else
        {
            PlayInstructionAudio(index);

            if (_slideDelays[index] > 0f)
                _slideRoutine = StartCoroutine(AutoAdvance(_slideDelays[index]));
        }
    }

    private IEnumerator PlaySlide3Instructions()
    {
        _instructionAudioSource.Stop();

        ResetSlide3Highlights();

        for (int i = 0; i < _slide3InstructionClips.Length; i++)
        {
            HighlightSlide3Instruction(i);

            _instructionAudioSource.clip = _slide3InstructionClips[i];
            _instructionAudioSource.Play();

            yield return new WaitForSeconds(_slide3InstructionClips[i].length);
        }

        ResetSlide3Highlights();
    }

    //private void HighlightSlide3Instruction(int index)
    //{
    //    for (int i = 0; i < _slide3InstructionTexts.Length; i++)
    //    {
    //        _slide3InstructionTexts[i].color =
    //            (i == index) ? _slide3HighlightColor : _slide3NormalColor;
    //    }

    //    for (int i = 0; i < _slide3InstructionRects.Length; i++)
    //    {
    //        if (i == index)
    //        {
    //            _slide3InstructionRects[i].localScale = Vector3.one * 1.1f;
    //            break;
    //        }
    //    }
    //}
    private void HighlightSlide3Instruction(int index)
    {
        // Text Color Update
        for (int i = 0; i < _slide3InstructionTexts.Length; i++)
        {
            // Condition: Agar current index i hai, YA index 3 hai aur i zero hai
            if (i == index || (index == 3 && i == 0))
            {
                _slide3InstructionTexts[i].color = _slide3HighlightColor;
            }
            else
            {
                _slide3InstructionTexts[i].color = _slide3NormalColor;
            }
        }

        // Scale (RectTransform) Update
        for (int i = 0; i < _slide3InstructionRects.Length; i++)
        {
            // Yahan bhi wahi logic: Index 3 par Element 0 ka scale bhi barhega
            if (i == index || (index == 3 && i == 0))
            {
                _slide3InstructionRects[i].localScale = Vector3.one * 1.1f;
            }
            else
            {
                _slide3InstructionRects[i].localScale = Vector3.one;
            }
        }
    }
    private void ResetSlide3Highlights()
    {
        for (int i = 0; i < _slide3InstructionTexts.Length; i++)
        {
            _slide3InstructionTexts[i].color = _slide3NormalColor;
        }

        for (int i = 0; i < _slide3InstructionRects.Length; i++)
        {
            _slide3InstructionRects[i].localScale = Vector3.one;
        }
    }

    private void HandleButtons()
    {
        _nextButton.gameObject.SetActive(_currentSlideIndex >= 2);
        _skipButton.gameObject.SetActive(_currentSlideIndex >= 3);
    }

    private void HandleGuideCharacter()
    {
        if (_currentSlideIndex < 2)
        {
            _guideCharacter.SetActive(false);
            return;
        }

        _guideCharacter.SetActive(true);

        if (_guidePositions != null &&
            _currentSlideIndex < _guidePositions.Length &&
            _guidePositions[_currentSlideIndex] != null)
        {
            _guideCharacter.transform.position =
                _guidePositions[_currentSlideIndex];
        }
    }

    private void PlayInstructionAudio(int index)
    {
        _instructionAudioSource.Stop();

        if (_instructionClips[index] == null)
            return;

        _instructionAudioSource.clip = _instructionClips[index];
        _instructionAudioSource.Play();
    }

    private IEnumerator AutoAdvance(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowSlide(_currentSlideIndex + 1);
    }

    private void OnNextPressed()
    {
        _buttonAudioSource.Play();

        if (_currentSlideIndex >= _slides.Length - 1)
        {
            ExitToMainMenu();
            return;
        }

        ShowSlide(_currentSlideIndex + 1);
    }

    private void OnSkipPressed()
    {
        _buttonAudioSource.Play();
        ExitToMainMenu();
    }

    private void ExitToMainMenu()
    {
        _instructionAudioSource.enabled = false;
        _guideCharacter.SetActive(false);
        _instructionAudioSource.Stop();
        _preBriefingPanel.SetActive(false);
        _mainMenuPanel.SetActive(true);
    }
}