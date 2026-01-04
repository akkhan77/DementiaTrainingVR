using UnityEngine;

public class ResultButtonHelper : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int rowIndex;
    public bool isYesButton;
    public KNPIAssessmentManager manager;

    public void OnClick()
    {
        manager.ProcessResultClick(rowIndex, isYesButton);
    }
}
