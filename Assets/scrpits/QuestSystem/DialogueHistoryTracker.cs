using System.Collections.Generic;
using UnityEngine;

public class DialogueHistoryTracker : MonoBehaviour
{
    private readonly HashSet<ActorSO> actorsSpokenWith =
        new HashSet<ActorSO>();

    public void RecordDialogue(ActorSO actor)
    {
        if (actor != null)
            actorsSpokenWith.Add(actor);
    }

    public bool HasSpokenWith(ActorSO actor)
    {
        return actor != null && actorsSpokenWith.Contains(actor);
    }
}