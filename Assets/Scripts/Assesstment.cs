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
    public GameObject nextPanel;
    [Header("Retry Settings")]
    private int resultRetryCount = 0;

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

    [Header("Result Sounds")]
    public AudioClip resultPositiveSound; // image_f64902.png (Question1_wrong.mp3 ya blinking sound)
    public AudioClip resultNegativeSound;
    [Header("Feedback Images")]
    [SerializeField] private GameObject _yesFeedbackImage; // Inspector mein Yes (Tick) image dalen
    [SerializeField] private GameObject _noFeedbackImage;  // Inspector mein No (Cross) image dalen
    [SerializeField] private float _feedbackDuration = 2.0f;
    [Header("Result Panel Feedback Icons")]
    public GameObject[] resultFeedbackIcons;
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

        // FIX: Saare question panels ko pehle band karein taake purana panel nazar na aaye
        foreach (var qData in questions)
        {
            if (qData.questionPanel != null)
                qData.questionPanel.SetActive(false);
        }
        if (questions.Count > 0 && questions[0].questionPanel != null)
        {
            questions[0].questionPanel.SetActive(true);
        }
        if (buttonsGroup != null) buttonsGroup.SetActive(false);

        // Intro audio logic
        if (introVoice != null)
        {
            guideAudioSource.clip = introVoice;
            guideAudioSource.Play();
            StartCoroutine(WaitAndStartFirstQuestion(introVoice.length));
        }
        else
        {
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
    private IEnumerator ShowFeedbackRoutine(GameObject imageToShow)
    {
        if (imageToShow != null)
        {
            imageToShow.SetActive(true);
        }

        yield return new WaitForSeconds(_feedbackDuration);

        if (imageToShow != null)
        {
            imageToShow.SetActive(false);
        }
    }
    public void OnResultButtonClick()
    {
        bool isPositive = CalculateSCAMResult();

        if (isPositive && resultRetryCount < 1)
        {
            // Pehli baar Positive aaya: Retry karwaein
            resultRetryCount++;
            StartAssessment(); // Sab reset karke pehle sawal par le jaye ga
        }
        else
        {
            // Ya to result Negative hai, ya phir retry ke baad bhi Positive hai
            // Dono suraton mein Aglay Panel par jayein
            resultsPanel.SetActive(false);
            if (nextPanel != null) {
            nextPanel.SetActive(true);

            }

            Debug.Log("Proceeding to Next Panel...");
        }
    }
    public void OnButtonClick(int choice)
    {
        AssessmentQuestion q = questions[currentIdx];

        // 1. Buttons ko Disable karein taake double click na ho, lekin gayab na hon
        // Buttons ke parent group par CanvasGroup component hona chahiye
        CanvasGroup cg = buttonsGroup.GetComponent<CanvasGroup>();
        if (cg != null) cg.interactable = false;

        // 2. Image Enable Karein
        GameObject imageToShow = (choice == 0) ? _yesFeedbackImage : _noFeedbackImage;
        StartCoroutine(ShowFeedbackAndTransition(imageToShow, choice == q.correctAnswerIndex, choice));
    }

    private IEnumerator ShowFeedbackAndTransition(GameObject img, bool isCorrect, int choice)
    {
        // Feedback image dikhayen
        if (img != null) img.SetActive(true);

        // Intezar karein taake user feedback dekh sakay
        yield return new WaitForSeconds(_feedbackDuration);

        // Image band karein
        if (img != null) img.SetActive(false);

        // AB buttons ko hide karein aur agla logic chalayen
        if (isCorrect)
        {
            if (buttonsGroup != null) buttonsGroup.SetActive(false);

            string label = (choice == 0) ? "Yes" : "No";
            history.Add($"{GetFeatureName(currentIdx)}: <b>{label}</b>");

            if (correctChime != null) guideAudioSource.PlayOneShot(correctChime);

            StartCoroutine(HandleCorrectAnswer());
        }
        else
        {
            // Ghalat jawab ka logic
            HandleWrongAnswer(choice);
        }

        // Buttons ko wapis interactable kardein agle sawal ke liye
        CanvasGroup cg = buttonsGroup.GetComponent<CanvasGroup>();
        if (cg != null) cg.interactable = true;
    }

    private void HandleWrongAnswer(int choice)
    {
        attemptCount++;
        if (attemptCount < 2)
        {
            StartCoroutine(ShowTryAgainFeedback());
        }
        else
        {
            if (buttonsGroup != null) buttonsGroup.SetActive(false);

            string label = (choice == 0) ? "Yes" : "No";
            history.Add($"{GetFeatureName(currentIdx)}: <b>{label} (Failed)</b>");
            attemptCount = 0;
            currentIdx++;
            UpdateUI();
        }
    }

    // Image ko thori dair dikha kar band karne ka helper method
    private IEnumerator ShowFeedbackImage(GameObject img)
    {
        if (img != null)
        {
            img.SetActive(true);
            yield return new WaitForSeconds(_feedbackDuration);
            img.SetActive(false);
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
        // Pehle saari 10 images ko band kar dein taake purana data saaf ho jaye
        foreach (GameObject icon in resultFeedbackIcons)
        {
            if (icon != null) icon.SetActive(false);
        }

        for (int i = 0; i < history.Count; i++)
        {
            bool isYes = history[i].Contains("Yes");

            // Buttons ke colors update karein
            if (i < featureYesImages.Length)
            {
                featureYesImages[i].color = isYes ? selectedColor : defaultColor;
                featureNoImages[i].color = !isYes ? selectedColor : defaultColor;
            }

            // --- Feedback Icons Logic ---
            if (resultFeedbackIcons != null && resultFeedbackIcons.Length >= 10)
            {
                if (isYes)
                {
                    // Agar Yes hai, to array ka index (i) on karein (0 se 4 tak)
                    resultFeedbackIcons[i].SetActive(true);
                }
                else
                {
                    // Agar No hai, to array ka index (i + 5) on karein (5 se 9 tak)
                    resultFeedbackIcons[i + 5].SetActive(true);
                }
            }
        }

        // Result Calculation (IsPositive)
        bool isPositive = CalculateSCAMResult();
        if (isPositive)
            finalResultsText.text = "<align=center><b>¼¶¸Á (+) ÆÇÁ¤</align>";
        else
            finalResultsText.text = "<align=center>¼¶¸Á (-) ÆÇÁ¤</align>";
    }

    bool CalculateSCAMResult()
        {
            foreach (string entry in history)
            {
                if (entry.Contains("Yes")) return true;
            }
            return false; // Sab "No" hain to hi Negative aayega
        }
        //if (history.Count < 5) return false;

        //// Index 0: Q1 (Box 1)
        //// Index 1: Q2 (Box 1)
        //// Index 2: Q3 (Inattention - Mandatory)
        //// Index 3: Q4 (Box 2)
        //// Index 4: Q5 (Box 2)

        //bool q1 = history[0].Contains("Yes");
        //bool q2 = history[1].Contains("Yes");
        //bool q3 = history[2].Contains("Yes"); // II. Inattention
        //bool q4 = history[3].Contains("Yes");
        //bool q5 = history[4].Contains("Yes");

        //// Logic: (Inattention) AND (Q1 OR Q2) AND (Q4 OR Q5)
        //bool box1 = q1 || q2;
        //bool box2 = q4 || q5;

        //return q3 && box1 && box2;
    

    void ShowSummary()
    {
        assessmentPanel.SetActive(false);
        resultsPanel.SetActive(true);
        RefreshResultUI();

        // Result check karein aur sahi sound play karein
        bool isPositive = CalculateSCAMResult();

        if (isPositive)
        {
            if (resultPositiveSound != null)
            {
                guideAudioSource.Stop(); // Purani audio band karein
                guideAudioSource.PlayOneShot(resultPositiveSound);
            }
        }
        else
        {
            if (resultNegativeSound != null)
            {
                guideAudioSource.Stop(); // Purani audio band karein
                guideAudioSource.PlayOneShot(resultNegativeSound);
            }
        }
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
        yield return new WaitForSeconds(1.5f);
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