using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class KNPIQuestion
{

    public string domainName;
    public string questionText;
    public AudioClip questionAudio;
    public bool isPresent; // Result of Yes/No
    public bool correctAnswer; // Right answer kya hona chahiye (Inspector mein set karein)
}

public class KNPIAssessmentManager : MonoBehaviour
{
    public KNPIQuestion[] questions = new KNPIQuestion[12];
    public TextMeshProUGUI uiQuestionText;
    public AudioSource audioSource;
    public TextMeshProUGUI uiNameText;

    [Header("Result Panel UI")]
    public GameObject resultPanel;
    public GameObject tryAgainPanel;
    private int currentQuestionIndex = 0;
    public TextMeshProUGUI[] resultCheckmarks;

    private bool hasTriedAgain = false;
    [Header("KNPI Assessment Audios")]
    public AudioClip knpiStart01; // Question2_start1
    public AudioClip knpiStart02; // Question2_start2
    public AudioClip tryAgainAudio;

    [Header("Result Panel Audios")]
    public AudioClip debriefingIntro; // 11.mp3
    public AudioClip aiFinalFeedback; // ai feedback10.mp3

    public void StartKNPISequence()
    {
        StartCoroutine(KNPIFlowRoutine());
    }

    IEnumerator KNPIFlowRoutine()
    {
        // 1. Play Start 1
        audioSource.clip = knpiStart01;
        audioSource.Play();
        yield return new WaitForSeconds(audioSource.clip.length);

        // 2. Play Start 2 (Instructions)
        audioSource.clip = knpiStart02;
        audioSource.Play();
        // Now enable the Yes/No buttons for the 12 questions
    }

    public void StartAssessment()
    {
        currentQuestionIndex = 0;
        ShowQuestion(currentQuestionIndex);
    }

    void ShowQuestion(int index)
    {
        uiNameText.text = questions[index].domainName;
        uiQuestionText.text = questions[index].questionText;
        audioSource.clip = questions[index].questionAudio;
        audioSource.Play();
    }

    public void OnClickAnswer(bool userSelection)
    {
        // 1. User ki choice save karein taake result mein dikh sake
        questions[currentQuestionIndex].isPresent = userSelection;

        // 2. Check karein ke answer sahi hai ya nahi
        if (userSelection == questions[currentQuestionIndex].correctAnswer)
        {
            HandleCorrectAnswer();
        }
        else
        {
            if (!hasTriedAgain)
            {
                ShowTryAgain();
            }
            else
            {
                HandleCorrectAnswer();
            }
        }
    }

    void ShowTryAgain()
    {
        hasTriedAgain = true; // Mark kar diya ke ek bar ghalat ho chuka hai
        tryAgainPanel.SetActive(true);

        if (tryAgainAudio != null)
        {
            audioSource.clip = tryAgainAudio;
            audioSource.Play();
        }
    }

    // Ye function tab chale jab user Try Again panel ka "Close" button dabaye
    public void CloseTryAgain()
    {
        tryAgainPanel.SetActive(false);
        audioSource.Stop(); // Panel band hote hi sound band
    }

    void HandleCorrectAnswer()
    {
        hasTriedAgain = false;
        currentQuestionIndex++;

        if (currentQuestionIndex < questions.Length)
            ShowQuestion(currentQuestionIndex);
        else
            ShowResultPanel();
    }

    void ShowResultPanel()
    {
        resultPanel.SetActive(true);
        UpdateResultTableUI(); // Table mein "v" lagane wala function
    }
    public void UpdateResultTableUI()
    {
        for (int i = 0; i < questions.Length; i++)
        {
            if (questions[i].isPresent)
            {
                resultCheckmarks[i].text = "v";      // Yes column
                resultCheckmarks[i + 12].text = "";  // No column empty
            }
            else
            {
                resultCheckmarks[i].text = "";       // Yes empty
                resultCheckmarks[i + 12].text = "v"; // No column
            }
        }
    }
    public void OnResultCellClick(int index, bool clickedYes)
    {
        // Agar user ne ghalt button dabaya (Correction ghalt ki)
        if (clickedYes != questions[index].correctAnswer)
        {
            PlayTryAgainSoundOnly();
        }
        else
        {
            // Agar sahi select kiya toh update kar dein
            questions[index].isPresent = clickedYes;
            UpdateResultTableUI();
        }
    }
    public void ProcessResultClick(int index, bool clickedYes)
    {
        // Check karein ke index range mein hai ya nahi (safety check)
        if (index < 0 || index >= questions.Length) return;

        // Agar user ne ghalt button dabaya
        if (clickedYes != questions[index].correctAnswer)
        {
            PlayTryAgainSoundOnly();
        }
        else
        {
            // Agar sahi select kiya toh "v" update karein
            questions[index].isPresent = clickedYes;
            UpdateResultTableUI();
        }
    }
    void PlayTryAgainSoundOnly()
    {
        audioSource.clip = tryAgainAudio;
        audioSource.Play();
        // Agar panel bhi dikhana hai toh: tryAgainPanel.SetActive(true);
    }
    public void StartDebriefingSequence()
    {
        StartCoroutine(DebriefingRoutine());
    }

    IEnumerator DebriefingRoutine()
    {
        // 0-5 sec: AI asks the challenge question (11.mp3)
        audioSource.clip = debriefingIntro;
        audioSource.Play();
        yield return new WaitForSeconds(5f);

        // 5-15 sec: User Reflection Time (Silence or "Listening" icon)
        Debug.Log("Waiting for user response...");
        yield return new WaitForSeconds(10f);

        // 15-25 sec: Final AI Summary (ai feedback10.mp3)
        audioSource.clip = aiFinalFeedback;
        audioSource.Play();
    }
}