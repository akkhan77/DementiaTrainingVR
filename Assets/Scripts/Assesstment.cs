using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Assesstment : MonoBehaviour
{
    [Header("Assessment Data")]
    public List<AssessmentQuestion> questions = new List<AssessmentQuestion>();
    private int currentIdx = 0;
    private List<string> history = new List<string>();
    private int attemptCount = 0;

    [Header("UI Panels")]
    public GameObject assessmentPanel;
    public GameObject resultsPanel;
    public GameObject tryAgainUI;
    public GameObject buttonsGroup; // Is group ko drag karein jo buttons ka parent hai
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI featureHeadingText;
    public TextMeshProUGUI finalResultsText;


    [Header("Audio")]
    public AudioSource guideAudioSource;
    public AudioClip introVoice;     // Panel khulne par pehli audio
    public AudioClip correctChime;
    public AudioClip tryAgainVoice;

    [Header("Result Panel Appearance")]
    public Image[] featureYesImages;
    public Image[] featureNoImages;
    public Color selectedColor = Color.red;
    public Color defaultColor = Color.white;


    void Start()
    {
        StartAssessment();
    }

    public void StartAssessment()
    {
        currentIdx = 0;
        history.Clear();
        assessmentPanel.SetActive(true);
        resultsPanel.SetActive(false);

        if (buttonsGroup != null) buttonsGroup.SetActive(false);

        // Intro audio play karein
        if (introVoice != null)
        {
            guideAudioSource.clip = introVoice;
            guideAudioSource.Play();

            // Audio ki length ke mutabiq wait karein
            StartCoroutine(WaitAndStartFirstQuestion(introVoice.length));
        }
        else
        {
            // Agar intro audio nahi hai to foran start karein
            UpdateUI();
        }
    }

    IEnumerator WaitAndStartFirstQuestion(float delay)
    {
        // Jitni der intro sound hai, utni der wait karein
        yield return new WaitForSeconds(delay);

        // Thoda extra gap (optional 0.5s) dena chahein to:
        yield return new WaitForSeconds(0.5f);

        UpdateUI();
    }

    void UpdateUI()
    {
        if (currentIdx < questions.Count)
        {
            // 1. Saare purane panels band karein (Safety ke liye)
            foreach (var qData in questions)
            {
                if (qData.questionPanel != null) qData.questionPanel.SetActive(false);
            }

            // 2. Sirf current question ka panel on karein
            AssessmentQuestion q = questions[currentIdx];
            if (q.questionPanel != null) q.questionPanel.SetActive(true);

            // 3. Audio handle karein
            if (q.questionAudio != null)
            {
                guideAudioSource.Stop(); // Purani audio band
                guideAudioSource.clip = q.questionAudio;
                guideAudioSource.Play();
            }

            // Buttons group ko on karein (agar wo panel ke bahar hai)
            if (buttonsGroup != null) buttonsGroup.SetActive(true);
        }
        else
        {
            ShowSummary();
        }
    }
    public void OnButtonClick(int choice)
    {
        AssessmentQuestion q = questions[currentIdx];

        if (choice == q.correctAnswerIndex)
        {
            // SAHI JAWAB: Buttons ko foran band kar dein
            if (buttonsGroup != null) buttonsGroup.SetActive(false);

            string label = (choice == 0) ? "Yes" : "No";
            history.Add($"{GetFeatureName(currentIdx)}: <b>{label}</b>");

            if (correctChime != null) guideAudioSource.PlayOneShot(correctChime);

            // 6 second wait karke agla sawal layein
            StartCoroutine(HandleCorrectAnswer());
        }
        else
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                StartCoroutine(ShowTryAgainFeedback());
            }
            else
            {
                // Doosri ghalti: Buttons band aur agla sawal
                if (buttonsGroup != null) buttonsGroup.SetActive(false);

                string label = (choice == 0) ? "Yes" : "No";
                history.Add($"{GetFeatureName(currentIdx)}: <b>{label} (Failed)</b>");
                attemptCount = 0;
                currentIdx++;
                UpdateUI();
            }
        }
    }

    // --- Result Panel Player Interactivity ---

    // Is function ko Result Panel ke buttons par lagayein
    public void ChangeResultInPanel(int index)
    {
        if (index >= history.Count) return;

        // Yes ko No, aur No ko Yes mein badlein
        if (history[index].Contains("Yes"))
            history[index] = $"{GetFeatureName(index)}: <b>No</b>";
        else
            history[index] = $"{GetFeatureName(index)}: <b>Yes</b>";

        RefreshResultUI();
    }

    void RefreshResultUI()
    {
        // 1. Buttons ke colors update karein
        // loop ko featureYesImages.Length tak chalayein taake saare buttons check hon
        for (int i = 0; i < featureYesImages.Length; i++)
        {
            // Check karein ke is index ki history exist karti hai
            if (i < history.Count)
            {
                bool isYes = history[i].Contains("Yes");

                // Yes Button ka color set karein
                featureYesImages[i].color = isYes ? selectedColor : defaultColor;

                // No Button ka color set karein
                featureNoImages[i].color = !isYes ? selectedColor : defaultColor;
            }
        }

        // 2. Algorithm Calculation
        bool isPositive = CalculateSCAMResult();

        // 3. Korean Text Update
        if (isPositive)
            finalResultsText.text = "<align=center><b>¼¶¸Á (+) ÆÇÁ¤</align>";
        else
            finalResultsText.text = "<align=center>¼¶¸Á (-) ÆÇÁ¤</align>";
    }

    bool CalculateSCAMResult()
    {
        if (history.Count < 4) return false;
        // Formula: (F1 AND F2) AND (F3 OR F4)
        bool f1 = history[0].Contains("Yes");
        bool f2 = history[1].Contains("Yes");
        bool f3 = history[2].Contains("Yes");
        bool f4 = history[3].Contains("Yes");
        return (f1 && f2) && (f3 || f4);
    }

    void ShowSummary()
    {
        assessmentPanel.SetActive(false);
        resultsPanel.SetActive(true);
        RefreshResultUI();
    }

    string GetFeatureName(int index)
    {
        switch (index)
        {
            case 0: return "Feature 1";
            case 1: return "Feature 2";
            case 2: return "Feature 3";
            case 3: return "Feature 4";
            default: return "Q" + index;
        }
    }

    IEnumerator HandleCorrectAnswer()
    {
        yield return new WaitForSeconds(2f);
        attemptCount = 0;
        currentIdx++;
        UpdateUI();
    }

    IEnumerator ShowTryAgainFeedback()
    {
        tryAgainUI.SetActive(true);
        if (tryAgainVoice != null) guideAudioSource.PlayOneShot(tryAgainVoice);
        yield return new WaitForSeconds(2f);
        tryAgainUI.SetActive(false);
    }

}
[System.Serializable]
public class AssessmentQuestion
{
    public string featureHeading;
    public string questionText;
    public AudioClip questionAudio; // Har question ki apni audio file
    public int correctAnswerIndex; // 0 for Yes, 1 for No
    public GameObject questionPanel; // Is question ka apna specific design panel
}