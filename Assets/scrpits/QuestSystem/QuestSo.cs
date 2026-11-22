using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName ="QuestSo",menuName ="QuestSo")]
public class QuestSo : ScriptableObject
{
    public string questName;
    public int questLevel;

    [TextArea] public string questDescription;

    public List<QuestObjective> objectives;
    public List<QuestReward> rewards;
}

[System.Serializable]
public class QuestObjective 
{
    public string description;

    [SerializeField] private Object target;
    public ItemSO TargetItem => target as ItemSO;
    public ActorSO TargetNPC => target as ActorSO;
    public LocationSO TargetLocation => target as LocationSO;

    public int requiredAmount;
   
}

[System.Serializable]
public class QuestReward
{
    public ItemSO itemSO;
    public int quantity;
}
