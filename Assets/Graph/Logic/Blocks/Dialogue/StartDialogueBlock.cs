using DialogueSystem.Logic;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Graph.Logic
{
    [Serializable]
    [BlockMenu("Dialogue/Start Dialogue")]
    public class StartDialogueBlock : GraphBlock
    {
        public override string DisplayName { get; set; } = "Start Dialogue";

        private List<DialogueOptionGraphPortPair> ports => dialogue.options.ConvertAll(option => new DialogueOptionGraphPortPair(option, new GraphOutputPort(option.text)));
        public override IReadOnlyList<GraphOutputPort> Ports => ports.ConvertAll(pair => pair.Port);

        public Dialogue dialogue;

        private List<Dialogue.DialogueOption> subscribedOptions = new List<Dialogue.DialogueOption>();

        public override void StartBlock()
        {
            DialogueManager.StartDialogue(dialogue);
            SubscribeOnEvents();
        }

        private void OnOptionSelected(Dialogue.DialogueOption option)
        {
            UnsubscribeFromEvents();
            Fire(GetPort(option));
        }

        private void SubscribeOnEvents()
        {
            foreach (var option in dialogue.options)
            {
                if (subscribedOptions.Contains(option)) continue;

                option.onSelected += OnOptionSelected;
                subscribedOptions.Add(option);
            }
        }

        private void UnsubscribeFromEvents()
        {
            for (int i = subscribedOptions.Count - 1; i >= 0; i--)
            {
                Dialogue.DialogueOption option = subscribedOptions[i];

                option.onSelected -= OnOptionSelected;
                subscribedOptions.Remove(option);
            }

        }



        private GraphOutputPort GetPort(Dialogue.DialogueOption option)
        {
            try
            {
                return ports.Find(pair => pair.Option == option).Port;
            }
            catch { return null; }
        }


        private class DialogueOptionGraphPortPair
        {
            public Dialogue.DialogueOption Option;
            public GraphOutputPort Port;

            public DialogueOptionGraphPortPair(Dialogue.DialogueOption option, GraphOutputPort port)
            {
                Option = option;
                Port = port;
            }
        }
    }
}