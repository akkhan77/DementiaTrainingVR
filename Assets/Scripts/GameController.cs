using System.Collections;
using UnityEngine;

public class GameController : MonoBehaviour
{
    // Stage 1-1 State Variables
    private bool handSanitized = false;
    public Animator handsnitizerAnim;
    public MeshRenderer sanitizerRenderer;
    private GameObject lastHighlightedObject;
    private Color originalColor;
    public GameObject braceletP;
    public GameObject knIPPanel;
    public GameObject quizP;
    public GameObject quizbtn;

    void Start()
    {
        
    }
    private void Update()
    {
        AudioSource[] sources = FindObjectsOfType<AudioSource>();

        foreach (AudioSource src in sources)
        {
            if (src.isPlaying && src.clip != null)
            {
                Debug.Log($"Playing Sound: {src.clip.name} | Object: {src.gameObject.name}");
            }
        }
    }
    public void Quiz_Btn()
    {
        knIPPanel.SetActive(false);
        quizbtn.SetActive(false);
        quizP.SetActive(true);

    }
    
    //// Update is called once per frame
    //void Update()
    //{
    //    Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    //    RaycastHit hit;

    //    if (Physics.Raycast(ray, out hit, 5f))
    //    {
    //        GameObject currentObject = hit.collider.gameObject;

    //        // Agar naya object hit hua hai
    //        if (currentObject != lastHighlightedObject)
    //        {
    //            ResetHighlight(); // Purane object ka rang wapis sahi karein

    //            lastHighlightedObject = currentObject;

    //            if (currentObject.GetComponent<Renderer>() != null)
    //            {
    //                originalColor = currentObject.GetComponent<Renderer>().material.color;
    //                currentObject.GetComponent<Renderer>().material.color = Color.yellow; // Highlight color
    //            }
    //        }

    //        Debug.Log("Hit: " + hit.collider.name + " | Layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));
    //    }
    //    else
    //    {
    //        ResetHighlight();
    //    }
    //}

    //void ResetHighlight()
    //{
    //    if (lastHighlightedObject != null)
    //    {
    //        lastHighlightedObject.GetComponent<Renderer>().material.color = originalColor;
    //        lastHighlightedObject = null;
    //    }
    //}

    public void OnHandSanitizerClick()
    {
        if (!handSanitized)
        {
            StartCoroutine(HandleSanitizationProcess());
        }
    }

    private IEnumerator HandleSanitizationProcess()
    {
        handSanitized = true;

        if (EpisodeOneFlowController.instance != null)
        {
            // 1. Sanitizer wala blinker (0) band karein
            EpisodeOneFlowController.instance.SetBlinker(0, false);
        }

        if (handsnitizerAnim != null)
        {
            handsnitizerAnim.enabled = true;
            yield return new WaitForSeconds(2.0f); // Animation ka wait
        }

        if (EpisodeOneFlowController.instance != null)
        {
            // 2. Player ko move karein
            EpisodeOneFlowController.instance.MovePlayerToNewPosition();

            // 3. State change karein
            EpisodeOneFlowController.instance.ChangeState(Episode1State.BraceletCheck, 0.1f);

            // 4. AB BRACELET WALA BLINKER (1) ON KAREIN
            EpisodeOneFlowController.instance.SetBlinker(1, true);
        }

        if (sanitizerRenderer != null) sanitizerRenderer.enabled = false;
    }

    public void deabug()
    {
        Debug.Log("sakjl");

    }
    public void Bracelet()
    {
        // Bracelet ka panel on karein
        braceletP.SetActive(true);

        // Timer aur Movement shuru karne ke liye Coroutine call karein
        StartCoroutine(HandleBraceletAndMoveBack());
    }
    private IEnumerator HandleBraceletAndMoveBack()
    {
        // 1. Pehle 4 seconds tak panel dikhayein
        yield return new WaitForSeconds(4f);

        // 2. Panel band karein
        braceletP.SetActive(false);

        if (EpisodeOneFlowController.instance != null)
        {
            // 3. Bracelet ka blinker (1) band karein
            EpisodeOneFlowController.instance.SetBlinker(1, false);

            // 4. Player ko wapis purani position par bhejein
            // Hum wahi function use karenge jo player ko move karta hai
            // Agar aapke paas start position ka variable hai to wo use karein
            EpisodeOneFlowController.instance.MovePlayerToStartPosition();

            // 5. Agli state (Introduction) par jayein
            EpisodeOneFlowController.instance.ChangeState(Episode1State.Introduction);
        }
    }
}

