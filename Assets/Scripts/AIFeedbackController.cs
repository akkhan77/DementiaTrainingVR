using TMPro;
using System;
using UnityEngine;
using System.Collections;

public class AIFeedbackController : MonoBehaviour
{
    [SerializeField] private GameObject _conversationCanvas;
    [SerializeField] private CanvasGroup _conversationIcon;
    [SerializeField] private GameObject _speakImg;
    [SerializeField] private GameObject _stopSpeakImg;
    [SerializeField] private TMP_Text _conversationText;
    [SerializeField] private float _timeout = 6f;
    [SerializeField] private AudioSource _feedbackSource;

    [Header("AiClips")]
    public AudioClip AiFeedbackClip01;
    public AudioClip AiFeedbackClip02;
    public AudioClip AiFeedbackClip03;

    AudioClip _feedbackClip;
    Coroutine timer;
    Coroutine _blinkCoroutine;
    bool _isBlinking = false;
    public event Action OnConversationEnded;
    public GameObject ConversationCanvas => _conversationCanvas;

    public void SetConversationText(string text)
    {
        _conversationText.text = text;
    }

    public void StartBlinking(float blinkSpeed = 1.7f, float minAlpha = 0.4f, float maxAlpha = 1f)
    {
        ToggleSpeakImages(true);
        if (_isBlinking && _blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
        }

        _isBlinking = true;
        _blinkCoroutine = StartCoroutine(BlinkRoutine(blinkSpeed, minAlpha, maxAlpha));
    }

    private void StopBlinking(bool hideIcon = true)
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        _isBlinking = false;

        if (hideIcon)
        {
            _conversationIcon.gameObject.SetActive(false);
        }
        else
        {
            _conversationIcon.alpha = 1f;
        }
    }

    public void StartListening(AudioClip audioClip)
    {
        _feedbackClip = audioClip;
        timer = StartCoroutine(FeedbackTimer(_feedbackClip));
    }

    private void StopListening()
    {
        ToggleSpeakImages(false);
        if (timer != null) StopCoroutine(timer);
        StopBlinking(false);
        SetConversationText(" ");
        OnConversationEnded?.Invoke();
    }

    IEnumerator FeedbackTimer(AudioClip audioClip)
    {
        yield return new WaitForSeconds(_timeout);
        _feedbackSource.PlayOneShot(audioClip);
        yield return new WaitForSeconds(audioClip.length);
        StopBlinking(false);
        StopListening();
    }

    private IEnumerator BlinkRoutine(float blinkSpeed, float minAlpha, float maxAlpha)
    {
        // Make sure icon is visible
        _conversationIcon.alpha = maxAlpha;
        _conversationIcon.gameObject.SetActive(true);

        float timer = 0f;
        bool fadingOut = true;

        while (true)
        {
            timer += Time.deltaTime * blinkSpeed;

            if (fadingOut)
            {
                _conversationIcon.alpha = Mathf.Lerp(maxAlpha, minAlpha, timer);
                if (timer >= 1f)
                {
                    fadingOut = false;
                    timer = 0f;
                }
            }
            else
            {
                _conversationIcon.alpha = Mathf.Lerp(minAlpha, maxAlpha, timer);
                if (timer >= 1f)
                {
                    fadingOut = true;
                    timer = 0f;
                }
            }

            yield return null;
        }
    }

    private void ToggleSpeakImages(bool isSpeaking)
    {
        if (isSpeaking)
        {
            _speakImg.SetActive(true);
            _stopSpeakImg.SetActive(false);
        }
        else
        {
            _stopSpeakImg.SetActive(true);
            _speakImg.SetActive(false);
        }
    }
}