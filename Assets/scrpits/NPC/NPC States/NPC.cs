using UnityEngine;

[DisallowMultipleComponent]
public class NPC : MonoBehaviour
{
    public enum NPCState
    {
        Default,
        Idle,
        Patrol,
        Wander,
        Talk
    }

    public NPCState currentState = NPCState.Patrol;
    public NPC_Patorl patorl;
    public NPC_Wander wander;
    public NPC_Talk talk;

    private NPCState defaultState;

    private void Awake()
    {
        if (patorl == null)
            patorl = GetComponent<NPC_Patorl>();

        if (wander == null)
            wander = GetComponent<NPC_Wander>();

        if (talk == null)
            talk = GetComponent<NPC_Talk>();

        defaultState = currentState;
    }

    private void Start()
    {
        SwitchState(currentState);
    }

    public void SwitchState(NPCState newState)
    {
        currentState = newState;

        if (patorl != null)
            patorl.enabled = newState == NPCState.Patrol;

        if (wander != null)
            wander.enabled = newState == NPCState.Wander;

        if (talk != null)
            talk.enabled = newState == NPCState.Talk;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (currentState != NPCState.Talk)
            SwitchState(NPCState.Talk);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (currentState == NPCState.Talk)
            SwitchState(defaultState);
    }
}
