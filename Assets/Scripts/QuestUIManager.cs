using UnityEngine;

public class QuestUIManager : MonoBehaviour
{
    public GameObject questPanel;

    public void ToggleQuestPanel()
    {
        questPanel.SetActive(!questPanel.activeSelf);
    }
}