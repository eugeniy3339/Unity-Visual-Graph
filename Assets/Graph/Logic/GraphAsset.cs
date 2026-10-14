using System.Collections.Generic;
using UnityEngine;

namespace Graph.Logic
{
    [CreateAssetMenu(fileName = "GraphAsset", menuName = "Graphs/Crete new Graph Asset")]
    public class GraphAsset : ScriptableObject
    {
        [SerializeReference]
        public List<GraphBlock> blocks = new List<GraphBlock>
        {
            new GraphBlock { DisplayName = "Entry", hasAnInputPort = false, position = new Vector2(200f, 200f) }
        };

        // ── Editor helpers ───────────────────────────────────────────────────────
        public IReadOnlyList<GraphBlock> GetBlocks() => blocks;

        public void AddBlock(GraphBlock block)
        {
            if (block == null || blocks.Contains(block)) return;
            blocks.Add(block);
        }

        public void RemoveBlock(GraphBlock block)
        {
            blocks.Remove(block);
        }

        // ── Runtime ──────────────────────────────────────────────────────────────
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
