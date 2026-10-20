using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Graph.Logic
{
    [CreateAssetMenu(fileName = "GraphAsset", menuName = "Graphs/Crete new Graph Asset")]
    public class GraphAsset : ScriptableObject
    {
        [SerializeReference] private List<GraphBlock> _blocks = new List<GraphBlock>
        {
            new GraphBlock { DisplayName = "Entry", HasInputPort = false, Position = new Vector2(200f, 200f) }
        };
        public List<GraphBlock> Blocks => _blocks;

        // ── Editor helpers ───────────────────────────────────────────────────────
        public IReadOnlyList<GraphBlock> GetBlocks() => Blocks;

        public void AddBlock(GraphBlock block)
        {
            if (block == null || Blocks.Contains(block)) return;
            Blocks.Add(block);
        }

        public void RemoveBlock(GraphBlock block)
        {
            Blocks.Remove(block);
        }

        // ── Runtime ──────────────────────────────────────────────────────────────
        public void StartGraph()
        {
            if (!CanStartGraph()) return;

            Blocks[0].StartBlock();
        }

        private bool CanStartGraph()
        {
            return Blocks.Count > 0;
        }



        public void SaveGraph()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
        }
    } 
}
