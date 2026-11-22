using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestLogUI : MonoBehaviour
{
    [Header("Quest Manager")]
    [SerializeField] private QuestManager questManager;

    [Header("Quest Details")]
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questDescriptionText;
    [SerializeField] private QuestObjectiveSlot[] objectiveSlots;
    [SerializeField] private QuestRewardSlot[] RewardSlots;

    [Header("Quest List")]
    [SerializeField] private QuestLogSlot[] questLogSlots;

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup questCanvas;
    [SerializeField] private CanvasGroup acceptCanvasGroup;
    [SerializeField] private CanvasGroup declineCanvasGroup;
    [SerializeField] private CanvasGroup completeCanvasGroup;

    private QuestSo questSo;
    [SerializeField] private QuestSo noAvailableQuestSO;

    // 本次运行期间，通过这个 UI 接取的任务。
    private readonly List<QuestSo> activeQuests =
        new List<QuestSo>();

    private void Awake()
    {
        if (questManager == null)
            questManager = FindObjectOfType<QuestManager>();

        SetCanvasState(acceptCanvasGroup, false);
        SetCanvasState(declineCanvasGroup, false);
        SetCanvasState(completeCanvasGroup, false);

        SetPanelVisible(false);
    }

    private void Start()
    {
        RefreshQuestList();
    }

    private void OnEnable()
    {
        QuestEvents.OnQuestOfferRequested += ShowQuestOffer;
        QuestEvents.OnQuestTurnInRequested += ShowQuestTurnIn;
    }

    private void OnDisable()
    {
        QuestEvents.OnQuestOfferRequested -= ShowQuestOffer;

        QuestEvents.OnQuestTurnInRequested -= ShowQuestTurnIn;
    }

    private void Update()
    {
        if (questCanvas != null && questCanvas.alpha > 0.5f)
        {
            RefreshQuestList();
            DisplayObjectives();
            RefreshButtons();
        }
        if (!BackpackUI.BlocksOtherInput && Input.GetKeyDown(KeyCode.K) && questCanvas != null)
        {
            bool visible = questCanvas.alpha < 0.5f;

            if (visible && questSo != null)
            {
                HandleQuestClicked(questSo);
            }

            SetPanelVisible(visible);
        }
    }

    public void ShowQuestOffer(QuestSo incomingQuestSO)
    {
        QuestSo displayedQuest =
            incomingQuestSO != null && questManager != null &&
            !questManager.GetCompleteQuests(incomingQuestSO)
                ? incomingQuestSO
                : noAvailableQuestSO;

        if (displayedQuest == null)
            return;

        HandleQuestClicked(displayedQuest);

        RefreshButtons();
        SetCanvasState(declineCanvasGroup, true);
        SetCanvasState(completeCanvasGroup, false);

        SetPanelVisible(true);
    }


    public void ShowQuestTurnIn(QuestSo incomingQuestSo) 
    {
        if (incomingQuestSo == null || questManager == null ||
            !questManager.IsQuestComplete(incomingQuestSo))
            return;
        HandleQuestClicked(incomingQuestSo);
        SetPanelVisible(true);
    }



    public void HandleQuestClicked(QuestSo selectedQuest)
    {
        if (selectedQuest == null)
            return;

        questSo = selectedQuest;

        if (questNameText != null)
            questNameText.text = questSo.questName;

        if (questDescriptionText != null)
            questDescriptionText.text = questSo.questDescription;

        DisplayObjectives();
        DisplayRewards();
        RefreshButtons();
    }

    public void OnAcceptQuestClicked()
    {
        if (questManager == null || questSo == null || questSo == noAvailableQuestSO)
            return;

        questManager.AcceptQuest(questSo);

        RefreshQuestList();
        DisplayObjectives();
        RefreshButtons();
    }

    public void OnDeclineQuestClicked()
    {
        SetPanelVisible(false);
    }

    public void OnComleteQuestClicked()
    {
        if (questManager == null || !questManager.TryCompleteQuest(questSo))
            return;
        RefreshQuestList();
        if (noAvailableQuestSO != null)
            HandleQuestClicked(noAvailableQuestSO);
        else
        {
            questSo = null;
            SetPanelVisible(false);
        }
        RefreshButtons();
    }

    public void RefreshQuestList()
    {
        activeQuests.Clear();
        if (questManager != null)
            activeQuests.AddRange(questManager.GetActiveQuests());
        if (questLogSlots == null)
            return;

        for (int i = 0; i < questLogSlots.Length; i++)
        {
            QuestLogSlot slot = questLogSlots[i];

            if (slot == null)
                continue;

            slot.questLogUI = this;

            if (i < activeQuests.Count)
            {
                slot.SetQues(activeQuests[i]);
            }
            else
            {
                slot.SetQues(null);
            }
        }
    }

    public void DisplayObjectives()
    {
        if (questSo == null || objectiveSlots == null)
            return;

        for (int i = 0; i < objectiveSlots.Length; i++)
        {
            QuestObjectiveSlot slot = objectiveSlots[i];

            if (slot == null)
                continue;

            if (questManager == null ||
                questSo.objectives == null ||
                i >= questSo.objectives.Count)
            {
                slot.gameObject.SetActive(false);
                continue;
            }

            var objective = questSo.objectives[i];

            questManager.UpdataObjectiveProgress(
                questSo,
                objective
            );

            int currentAmount = questManager.GetCurrentAmount(
                questSo,
                objective
            );

            string progress = questManager.GetProgressText(
                questSo,
                objective
            );

            bool isComplete =
                currentAmount >= objective.requiredAmount;

            slot.gameObject.SetActive(true);

            slot.RefreshObjectives(
                objective.description,
                progress,
                isComplete
            );
        }
    }

    private void DisplayRewards()
    {
        if (questSo == null || RewardSlots == null)
            return;

        for (int i = 0; i < RewardSlots.Length; i++)
        {
            QuestRewardSlot slot = RewardSlots[i];

            if (slot == null)
                continue;

            if (questSo.rewards == null ||
                i >= questSo.rewards.Count)
            {
                slot.gameObject.SetActive(false);
                continue;
            }

            var reward = questSo.rewards[i];

            slot.DisplayReward(
                reward.itemSO != null ? reward.itemSO.icon : null,
                reward.quantity
            );

            slot.gameObject.SetActive(true);
        }
    }

    private void RefreshButtons()
    {
        bool hasQuest = questSo != null && questSo != noAvailableQuestSO &&
            questManager != null && !questManager.GetCompleteQuests(questSo);
        bool accepted =
            hasQuest && questManager.IsQuestAccepted(questSo);

        SetCanvasState(
            acceptCanvasGroup,
            hasQuest && !accepted
        );

        SetCanvasState(
            declineCanvasGroup,
            hasQuest && !accepted
        );

        SetCanvasState(
            completeCanvasGroup,
            accepted && AreAllObjectivesComplete()
        );
    }

    private bool AreAllObjectivesComplete()
    {
        return questManager != null && questManager.IsQuestComplete(questSo);
    }

    private void SetCanvasState(
        CanvasGroup group,
        bool activate)
    {
        if (group == null)
            return;

        group.alpha = activate ? 1f : 0f;
        group.interactable = activate;
        group.blocksRaycasts = activate;
    }

    private void SetPanelVisible(bool visible)
    {
        SetCanvasState(questCanvas, visible);
    }

    public void ClosePanel()
    {
        SetPanelVisible(false);
    }
}