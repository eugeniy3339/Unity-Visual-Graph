using System.Collections.Generic;
using UnityEngine;

namespace Graph.Logic
{
    [CreateAssetMenu(fileName = "GraphAsset", menuName = "Graphs/Crete new Graph Asset")]
    public class GraphAsset : ScriptableObject
    {
        private List<GraphBlock> blocks = new List<GraphBlock>
        {
            new GraphBlock { DisplayName = "Entry", hasAnInputPort = false }
        };

        public void StartGraph()
        {
            if (!CanStartGraph()) return;

            blocks[0].StartBlock();
        }

        private bool CanStartGraph()
        {
            return blocks.Count > 0;
        }
    }
}
