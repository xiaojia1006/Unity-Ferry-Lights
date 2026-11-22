using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    public Image portrait;
    public TMP_Text actorName;
    public TMP_Text dialogueText;
    public Button[] choiceButtons;
    public CanvasGroup canvasGroup;

    public bool isDialogueActive;

    public DialogueSO currentDialogue;

    private int dialogueIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SetDialogueVisible(false);
        ClearChoices();
    }

    public void StarDialogue(DialogueSO dialogueSO)
    {
        if (dialogueSO == null ||
            dialogueSO.lines == null ||
            dialogueSO.lines.Length == 0)
        {
            EndDialogue();
            return;
        }

        currentDialogue = dialogueSO;
        dialogueIndex = 0;
        isDialogueActive = true;

        ClearChoices();
        ShowDialogue();
    }

    public void AdvanceDialogue()
    {
        if (currentDialogue == null ||
            currentDialogue.lines == null)
        {
            EndDialogue();
            return;
        }

        if (dialogueIndex < currentDialogue.lines.Length)
            ShowDialogue();
        else
            ShowChoices();
    }

    private void ShowDialogue()
    {
        if (currentDialogue == null ||
            currentDialogue.lines == null ||
            dialogueIndex < 0 ||
            dialogueIndex >= currentDialogue.lines.Length)
        {
            ShowChoices();
            return;
        }

        DialogueLine line = currentDialogue.lines[dialogueIndex];

        if (line != null && line.speaker != null)
        {
            if (portrait != null)
            {
                portrait.sprite = line.speaker.portait;
                portrait.enabled = line.speaker.portait != null;
            }

            if (actorName != null)
            {
                actorName.text = string.IsNullOrWhiteSpace(
                    line.speaker.actorName
                )
                    ? line.speaker.name
                    : line.speaker.actorName;
            }
        }
        else
        {
            if (portrait != null)
                portrait.enabled = false;

            if (actorName != null)
                actorName.text = "";
        }

        if (dialogueText != null)
            dialogueText.text = line != null ? line.text : "";

        dialogueIndex++;
        SetDialogueVisible(true);
    }

    public void EndDialogue()
    {
        dialogueIndex = 0;
        isDialogueActive = false;

        ClearChoices();
        SetDialogueVisible(false);
    }

    private void ClearChoices()
    {
        if (choiceButtons == null)
            return;

        foreach (Button button in choiceButtons)
        {
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();
            button.gameObject.SetActive(false);
        }
    }

    private void ShowChoices()
    {
        ClearChoices();

        if (currentDialogue != null &&
            currentDialogue.options != null &&
            currentDialogue.options.Length > 0)
        {
            int count = Mathf.Min(
                currentDialogue.options.Length,
                choiceButtons != null ? choiceButtons.Length : 0
            );

            for (int i = 0; i < count; i++)
            {
                DialogueOption option = currentDialogue.options[i];
                Button button = choiceButtons[i];

                TMP_Text buttonText =
                    button.GetComponentInChildren<TMP_Text>();

                if (buttonText != null)
                    buttonText.text = option.optionTxet;

                button.gameObject.SetActive(true);
                button.onClick.AddListener(
                    () => ChooseOption(option.nexDialogue)
                );
            }

            return;
        }

        if (choiceButtons != null &&
            choiceButtons.Length > 0 &&
            choiceButtons[0] != null)
        {
            Button endButton = choiceButtons[0];
            TMP_Text endText =
                endButton.GetComponentInChildren<TMP_Text>();

            if (endText != null)
                endText.text = "End";

            endButton.gameObject.SetActive(true);
            endButton.onClick.AddListener(EndDialogue);
        }
        else
        {
            EndDialogue();
        }
    }

    private void ChooseOption(DialogueSO dialogueSO)
    {
        if (dialogueSO == null)
            EndDialogue();
        else
            StarDialogue(dialogueSO);
    }

    private void SetDialogueVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
    private void Update()
    {
        if (GlobalSettings.BlocksInput) return;
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Escape))
        {
            EndDialogue();
        }
    }
}
