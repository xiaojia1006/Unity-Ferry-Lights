using System;
using UnityEngine;

public static class QuestEvents 
{
    public static Action<QuestSo> OnQuestOfferRequested;

    public static Action<QuestSo> OnQuestTurnInRequested;

    public static Func<QuestSo, bool> IsQuestComplete;
}
