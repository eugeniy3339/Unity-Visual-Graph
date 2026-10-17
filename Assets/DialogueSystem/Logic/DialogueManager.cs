using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem.Logic
{
    public static class DialogueManager
    {
        private static Dialogue curDialogue;

        public static Action<Dialogue> onDialogueStarted;
        public static Action onDialogueEnded;

        public static void StartDialogue(Dialogue dialogue)
        {
            curDialogue = dialogue;
            onDialogueStarted?.Invoke(dialogue);
        }

        public static void EndDialogue()
        {
            curDialogue = null;
            onDialogueEnded?.Invoke();
        }
    }

    [Serializable]
    public class Dialogue
    {
        [TextArea] public string text;
        public List<DialogueOption> options = new List<DialogueOption>();

        [Serializable]
        public class DialogueOption
        {
            public string text;
            public Action<DialogueOption> onSelected;

            public void Select()
            {
                onSelected?.Invoke(this);
            }
        }
    }
}
