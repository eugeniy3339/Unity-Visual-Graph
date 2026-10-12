using eugeniy3339.Dialogue;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[BlockMenu("Dialogue/Start Dialogue")]
public class StartDialogueBlock : GraphBlock
{
    public override string DisplayName => "Start Dialogue";

    [SerializeField] public Dialogue dialogue;
    private List<Dialogue.DialogueOption> subscribedOptions = new List<Dialogue.DialogueOption>();

    private List<string> ports => dialogue.options.ConvertAll(option => option.text);
    public override IReadOnlyList<string> OutputPorts => ports;

    public override void StartBlock()
    {
        DialogueManager.StartDialogue(dialogue);
        SubscribeToEvents();

        Fire();
    }

    private void SubscribeToEvents()
    {
        foreach(var dialogueOption in dialogue.options)
        {
            if (subscribedOptions.Contains(dialogueOption)) continue;
            dialogueOption.onSelected += OnOptionSelected;
            subscribedOptions.Add(dialogueOption);
        }
    }

    private void UnsubscribeFromEvents()
    {
        for (int i = subscribedOptions.Count - 1; i >= 0; i--)
        {
            subscribedOptions[i].onSelected -= OnOptionSelected;
            subscribedOptions.RemoveAt(i);
        }
    }

    private void OnOptionSelected(Dialogue.DialogueOption option)
    {
        UnsubscribeFromEvents();
        Fire(option.text);
    }
}