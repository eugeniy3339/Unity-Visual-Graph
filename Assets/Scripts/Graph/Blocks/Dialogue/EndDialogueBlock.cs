using System;
using eugeniy3339.Dialogue;

[Serializable]
[BlockMenu("Dialogue/End Dialogue")]
public class EndDialogueBlock : GraphBlock
{
    public override string DisplayName => "End Dialogue";

    public override void StartBlock()
    {
        DialogueManager.EndDialogue();
        Fire();
    }
}