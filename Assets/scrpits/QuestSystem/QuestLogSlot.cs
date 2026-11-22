using UnityEngine;
using TMPro;

public class QuestLogSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questLevelText;

    public QuestLogUI questLogUI;
    public QuestSo currentQuest;

    private void OnValidate()
    {
        RefreshText();
    }

    public void SetQues(QuestSo questSo)
    {
        currentQuest = questSo;

        RefreshText();
        gameObject.SetActive(currentQuest != null);
    }

    private void RefreshText()
    {
        if (questNameText != null)
        {
            questNameText.text = currentQuest != null
                ? currentQuest.questName
                : "";
        }

        if (questLevelText != null)
        {
            questLevelText.text = currentQuest != null
                ? "Lv:" + currentQuest.questLevel
                : "";
        }
    }

    public void ClearSlot()
    {
        SetQues(null);
    }

    public void OnSlotClicked()
    {
        if (currentQuest == null || questLogUI == null)
            return;

        questLogUI.HandleQuestClicked(currentQuest);
    }
}