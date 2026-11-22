using UnityEngine;

[DisallowMultipleComponent]
public class NPC_Talk : MonoBehaviour
{
    public Rigidbody2D rb;
    public Animator anim;
    public Animator interactAnim;
    public DialogueSO dialogueSO;

    private bool wasKinematic;
    private bool capturedKinematicState;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (anim == null)
        {
            Transform sprite = transform.Find("Sprite");

            if (sprite != null)
                anim = sprite.GetComponent<Animator>();
            if (anim == null)
                anim = GetComponent<Animator>();
        }

        if (interactAnim == null)
        {
            Transform interaction = transform.Find("Interaction");

            if (interaction != null)
                interactAnim = interaction.GetComponent<Animator>();
        }
    }

    private void OnEnable()
    {
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            capturedKinematicState = true;
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        PlayState(anim, "BluePawn_Idle");

        if (interactAnim != null)
        {
            interactAnim.enabled = true;
            PlayState(interactAnim, "Open");
        }
    }

    private void OnDisable()
    {
        PlayState(interactAnim, "Close");

        if (rb != null && capturedKinematicState)
            rb.isKinematic = wasKinematic;

        capturedKinematicState = false;
    }

    private void Update()
    {
        if (DialogueManager.Instance == null)
            return;

        if (BackpackUI.BlocksOtherInput || !Input.GetButtonDown("Interact"))
            return;

        if (DialogueManager.Instance.isDialogueActive)
            DialogueManager.Instance.AdvanceDialogue();
        else
            DialogueManager.Instance.StarDialogue(dialogueSO);
    }

    private static void PlayState(Animator target, string stateName)
    {
        if (target == null ||
            target.runtimeAnimatorController == null || !target.gameObject.activeInHierarchy ||
            !target.HasState(0, Animator.StringToHash(stateName)))
        {
            return;
        }

        target.enabled = true;
        target.Play(stateName, 0);
    }
}
