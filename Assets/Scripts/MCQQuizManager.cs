using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class MCQQuizManager : MonoBehaviour
{
    [Header("Question Data")]
    public string questionStr = "As a nurse, what is the most appropriate initial action?";
    public string[] options = { "Call the doctor", "Administer sedative", "Assess for BPSD/DSD", "Restrain the patient" };
    public int correctOptionIndex = 0; // Index 2 matlab "Assess for BPSD/DSD" sahi hai
    [Header("Audio")]
    public AudioSource aiAudio;
    public AudioClip questionAudio;
    public AudioClip tryAgainAudio;
    public AudioClip correctFeedbackAudio; // Naya: Correct answer batane wala audio
    [Header("UI References")]
    public TextMeshProUGUI questionDisplay;
    public Button[] optionButtons;
    public GameObject tryAgainPanel;
    public GameObject nextStagePanel; // Jo Quiz ke baad khulega
    [Header("UI Feedback")]
    public Color correctButtonColor = new Color(0f, 0f, 0.5f); // Dark Blue
    private Color initialButtonColor;
    private bool hasFailedOnce = false;

    void Start()
    {
        SetupQuiz();
    }

    void SetupQuiz()
    {
        questionDisplay.text = questionStr;
        aiAudio.clip = questionAudio;
        aiAudio.Play();

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i; // Closure for listener
            optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = options[i];
            optionButtons[i].onClick.AddListener(() => OnOptionClick(index));
        }
    }

    public void OnOptionClick(int selectedIndex)
    {
        if (selectedIndex == correctOptionIndex)
        {
            // Pehli bar mein hi sahi jawab
            ShowCorrectAndProceed();
        }
        else
        {
            if (!hasFailedOnce)
            {
                // Pehli ghalti: Try Again
                ShowTryAgain();
            }
            else
            {
                // Doosri ghalti: Sahi jawab dikhao aur agay barho
                StartCoroutine(ForceCorrectAnswerFeedback());
            }
        }
    }

    IEnumerator ForceCorrectAnswerFeedback()
    {
        // 1. Sahi button ka color dark blue karein
        Image btnImage = optionButtons[correctOptionIndex].GetComponent<Image>();
        initialButtonColor = btnImage.color;
        btnImage.color = correctButtonColor;

        // 2. Correct audio play karein
        if (correctFeedbackAudio != null)
        {
            aiAudio.Stop();
            aiAudio.PlayOneShot(correctFeedbackAudio);
            yield return new WaitForSeconds(correctFeedbackAudio.length);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        // 3. Next stage par jayein
        ProceedToNextStage();
    }

    public void ShowCorrectAndProceed()
    {
        // Direct sahi jawab dene par feedback
        if (correctFeedbackAudio != null) aiAudio.PlayOneShot(correctFeedbackAudio);
        //yield return new WaitForSeconds(1.5f);
        //ProceedToNextStage();
    }

    void ShowTryAgain()
    {
        hasFailedOnce = true;
        tryAgainPanel.SetActive(true);

        if (tryAgainAudio != null)
        {
            aiAudio.clip = tryAgainAudio;
            aiAudio.Play();

            // Jitni der ki audio hai utni der wait karke hide karein
            StartCoroutine(HideTryAgainAfterDelay(tryAgainAudio.length));
        }
        else
        {
            // Agar audio nahi hai to 2 second baad hide karein
            StartCoroutine(HideTryAgainAfterDelay(1.5f));
        }
    }

    IEnumerator HideTryAgainAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        tryAgainPanel.SetActive(false);
    }

    public void CloseTryAgain()
    {
        tryAgainPanel.SetActive(false);
        aiAudio.Stop(); // Panel band hote hi audio band
    }

    void ProceedToNextStage()
    {
        this.gameObject.SetActive(false);
        nextStagePanel.SetActive(true); // Ye aapka Page 65 wala Result Panel ho sakta hai
    }
}