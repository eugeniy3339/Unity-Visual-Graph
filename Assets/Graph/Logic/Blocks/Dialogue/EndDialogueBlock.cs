using DialogueSystem.Logic;

namespace Graph.Logic
{
    [BlockMenu("Dialogue/End Dialogue")]
    public class EndDialogueBlock : GraphBlock
    {
        public override string DisplayName { get; set; } = "End Dialogue";

        public override void StartBlock()
        {
            DialogueManager.EndDialogue();
            Fire();
        }
    }
}