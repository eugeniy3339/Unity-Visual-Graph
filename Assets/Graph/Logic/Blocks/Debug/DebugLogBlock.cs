using System;
using UnityEngine;

namespace Graph.Logic
{
    [Serializable]
    [BlockMenu("Debug/Log")]
    public class DebugLogBlock : GraphBlock
    {
        public override string DisplayName { get; set; } = "Debug Log";

        [SerializeField, TextArea] private string debugText = "Hello World";

        public override void StartBlock()
        {
            Debug.Log(debugText);
            base.StartBlock();
        }
    }
}
