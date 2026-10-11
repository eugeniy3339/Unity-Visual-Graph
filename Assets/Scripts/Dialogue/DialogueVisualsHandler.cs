using UnityEngine;
using eugeniy3339.Dialogue;
using TMPro;
using System.Collections.Generic;

public class DialogueVisualsHandler : MonoBehaviour
{
    [SerializeField] private GameObject dialogueOptionPrefab;

    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Transform optionsContainer;
    [SerializeField] private TMP_Text dialogueText;

    private List<DialogueOptionHandler> spawnedDialogueOptionHandlers = new List<DialogueOptionHandler>();

    private void OnDialogueStarted(Dialogue dialogue)
    {
        dialoguePanel.SetActive(true);
        dialogueText.text = dialogue.text;
        SpawnDialogueOptions(dialogue.options);
    }

    private void OnDialogueEnded()
    {
        dialoguePanel.SetActive(false);
        dialogueText.text = string.Empty;
        DespawnCurDialogueOptions();
    }

    private void SpawnDialogueOptions(List<Dialogue.DialogueOption> dialogueOptions)
    {
        DespawnCurDialogueOptions();
        foreach(var dialogueOption in dialogueOptions)
        {
            SpawnDialogueOption(dialogueOption);
        }
    }

    private void SpawnDialogueOption(Dialogue.DialogueOption dialogueOption)
    {
        spawnedDialogueOptionHandlers.Add(DialogueOptionHandler.SpawnDialogueOptionHandler(dialogueOptionPrefab, dialogueOption, optionsContainer));
    }

    private void DespawnCurDialogueOptions()
    {
        for(int i = spawnedDialogueOptionHandlers.Count - 1; i >= 0; i--)
        {
            Destroy(spawnedDialogueOptionHandlers[i].gameObject);
            spawnedDialogueOptionHandlers.RemoveAt(i);
        }
    }

    private void OnEnable()
    {
        DialogueManager.onDialogueStarted += OnDialogueStarted;
        DialogueManager.onDialogueEnded += OnDialogueEnded;
    }

    private void OnDisable()
    {
        DialogueManager.onDialogueStarted -= OnDialogueStarted;
        DialogueManager.onDialogueEnded -= OnDialogueEnded;
    }
}
