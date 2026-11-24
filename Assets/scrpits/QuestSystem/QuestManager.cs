using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    private readonly Dictionary<
        QuestSo,
        Dictionary<QuestObjective, int>
    > quesProgress =
        new Dictionary<
            QuestSo,
            Dictionary<QuestObjective, int>
        >();

    private List<QuestSo> completedQuests = new();

    private readonly List<QuestSo> activeQuests =
        new List<QuestSo>();


    //public bool IsQuestAccepted(QuestSo questSo)
    //{
    //     return quesProgress.ContainsKey(questSo);
    //  }

    private void OnEnable()
    {
        QuestEvents.IsQuestComplete += IsQuestComplete;
       
    }

    private void OnDisable()
    {
        QuestEvents.IsQuestComplete -= IsQuestComplete;

       
    }

  

    public void AcceptQuest(QuestSo questSo)
    {
        if (questSo == null || activeQuests.Contains(questSo) || completedQuests.Contains(questSo))
            return;

        activeQuests.Add(questSo);

        if (!quesProgress.ContainsKey(questSo))
        {
            quesProgress[questSo] =
                new Dictionary<QuestObjective, int>();
        }

        if (questSo.objectives != null)
        {
            foreach (var objective in questSo.objectives)
            {
                UpdataObjectiveProgress(questSo, objective);
            }
        }
    }

    public List<QuestSo> GetActiveQuests()
    {
        return new List<QuestSo>(activeQuests);
    }

    public bool IsQuestAccepted(QuestSo questSo)
    {
        return questSo != null && activeQuests.Contains(questSo);
    }

    public void UpdataObjectiveProgress(
        QuestSo questSo,
        QuestObjective objective)
    {
        if (questSo == null || objective == null)
            return;

        if (!quesProgress.ContainsKey(questSo))
        {
            return;
        }

        int newAmount = 0;

        if (objective.TargetItem != null)
        {
            if (InventortyManager.Instance != null)
            {
                newAmount =
                    InventortyManager.Instance.GetItemQuantity(
                        objective.TargetItem
                    );
            }
        }
        else if (objective.TargetLocation != null)
        {
            if (GameManager.Instance != null &&
                GameManager.Instance.LoacationHistoyrTracker != null &&
                GameManager.Instance.LoacationHistoyrTracker
                    .HasVisited(objective.TargetLocation))
            {
                newAmount = objective.requiredAmount;
            }
        }
        else if (objective.TargetNPC != null)
        {
            if (GameManager.Instance != null &&
                GameManager.Instance.DialogueHistoryTracker != null &&
                GameManager.Instance.DialogueHistoryTracker
                    .HasSpokenWith(objective.TargetNPC))
            {
                newAmount = objective.requiredAmount;
            }
        }

        quesProgress[questSo][objective] = newAmount;
    }

    public string GetProgressText(
        QuestSo questSo,
        QuestObjective objective)
    {
        if (questSo == null || objective == null)
            return "";

        int currentAmount = GetCurrentAmount(
            questSo,
            objective
        );

        if (currentAmount >= objective.requiredAmount)
            return "Complete";

        if (objective.TargetItem != null)
            return $"{currentAmount}/{objective.requiredAmount}";

        return "In Progress";
    }

    public int GetCurrentAmount(
        QuestSo questSo,
        QuestObjective objective)
    {
        if (questSo == null || objective == null)
            return 0;

        if (quesProgress.TryGetValue(
                questSo, out var objectiveDictionary) &&
            objectiveDictionary.TryGetValue(
                objective, out int amount))
        {
            return amount;
        }

        return 0;
    }

    public bool IsQuestComplete(QuestSo questSo)
    {
        if (!IsQuestAccepted(questSo) ||
            questSo.objectives == null ||
            questSo.objectives.Count == 0)
        {
            return false;
        }

        foreach (var objective in questSo.objectives)
        {
            if (objective == null)
                return false;

            UpdataObjectiveProgress(questSo, objective);

            if (GetCurrentAmount(questSo, objective) <
                objective.requiredAmount)
            {
                return false;
            }
        }

        return true;
    }

    public void CompleteQuest(QuestSo questSo) 
    {
        TryCompleteQuest(questSo);
    }

    public bool TryCompleteQuest(QuestSo questSo)
    {
        if (!IsQuestComplete(questSo) || completedQuests.Contains(questSo))
            return false;

        var rewards = new Dictionary<ItemSO, int>();
        if (questSo.rewards != null)
        {
            foreach (var reward in questSo.rewards)
            {
                if (reward == null || reward.itemSO == null || reward.quantity <= 0)
                    return false;
                rewards.TryGetValue(reward.itemSO, out int quantity);
                if ((long)quantity + reward.quantity > int.MaxValue)
                    return false;
                rewards[reward.itemSO] = quantity + reward.quantity;
            }
        }

        var inventory = InventortyManager.Instance;
        if (rewards.Count > 0 && (inventory == null || !inventory.CanAddItems(rewards)))
        {
            Debug.LogWarning("Cannot claim quest rewards. Check inventory space.");
            return false;
        }

        quesProgress.Remove(questSo);
        activeQuests.Remove(questSo);
        completedQuests.Add(questSo);
        foreach (var reward in rewards)
            inventory.AddItem(reward.Key, reward.Value);
        PlayerStats.AddQuest("Legacy."+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+"."+questSo.name);
        return true;
    }

    public bool GetCompleteQuests(QuestSo questSo)
    {
        return completedQuests.Contains(questSo);
    }
}