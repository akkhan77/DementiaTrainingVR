using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class MCQQuizManager : MonoBehaviour
{
    [Header("Question Data")]
    public string questionStr = "As a nurse, what is the most appropriate initial action?";
    public string[] options = { "Call the doctor", "Administer sedative", "Assess for BPSD/DSD", "Restrain the patient" };
    public int correctOptionIndex = 2; // Index 2 matlab "Assess for BPSD/DSD" sahi hai

    [Header("UI References")]
    public TextMeshProUGUI questionDisplay;
    public Button[] optionButtons;
    public GameObject tryAgainPanel;
    public GameObject nextStagePanel; // Jo Quiz ke baad khulega

    [Header("Audio")]
    public AudioSource aiAudio;
    public AudioClip questionAudio; // Jo screen khulne par bajega
    public AudioClip tryAgainAudio; // Ghalat jawab par "Try Again" sound

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
            // Sahi Jawab
            ProceedToNextStage();
        }
        else
        {
            // Ghalat Jawab
            if (!hasFailedOnce)
            {
                ShowTryAgain();
            }
            else
            {
                // Doosri baar ghalat: Result save karo aur agay barho
                ProceedToNextStage();
            }
        }
    }

    void ShowTryAgain()
    {
        hasFailedOnce = true;
        tryAgainPanel.SetActive(true);
        aiAudio.clip = tryAgainAudio;
        aiAudio.Play();
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