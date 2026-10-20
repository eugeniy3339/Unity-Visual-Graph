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

        // Ports used to be re-created on every call, so their stored connections were lost.
        // Now they live in the serialized _ports array (one port per option) and are only
        // added / renamed / removed when the dialogue options change.
        public override IReadOnlyList<GraphOutputPort> Ports
        {
            get
            {
                SyncPorts();
                return _ports;
            }
        }

        private void SyncPorts()
        {
            int curCount = _ports != null ? _ports.Length : 0;
            int desiredCount = dialogue != null ? (dialogue.options != null ? dialogue.options.Count : 0) : 0;

            var result = new GraphOutputPort[desiredCount];

            for(int i = 0; i < desiredCount; i++)
            {
                if (_ports.Length > i)
                    result[i] = _ports[i];
                else
                    result[i] = new GraphOutputPort();

                result[i].Name = dialogue.options[i].text;
            }

            _ports = result;
        }

        public Dialogue dialogue;

        private List<Dialogue.DialogueOption> subscribedOptions = new List<Dialogue.DialogueOption>();

        public override void StartBlock()
        {
            DialogueManager.StartDialogue(dialogue);
            SubscribeOnEvents();
        }

        private void OnOptionSelected(Dialogue.DialogueOption option)
        {
            var port = GetPort(option);
            if (port == null) return;
            UnsubscribeFromEvents();
            Fire(port);
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
                int index = dialogue.options.IndexOf(option);
                var current = Ports;
                return index >= 0 && index < current.Count ? current[index] : null;
            }
            catch { return null; }
        }
    }
}