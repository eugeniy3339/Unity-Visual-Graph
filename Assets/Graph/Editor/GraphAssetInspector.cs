using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Graph.Logic;

namespace Graph.Editor
{
    /// <summary>
    /// Custom Inspector for GraphAsset.
    /// Shows an "Open Graph" button and handles double-click in the Project window.
    /// </summary>
    [CustomEditor(typeof(GraphAsset))]
    public class GraphAssetInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var asset = (GraphAsset)target;

            // "Open Graph" button — prominent, full-width
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 13,
                fontStyle = FontStyle.Bold,
                fixedHeight = 30
            };

            if (GUILayout.Button("Open Graph", buttonStyle))
            {
                GraphEditorWindow.Open(asset);
            }

            if(GUILayout.Button("Debug Graph", buttonStyle))
                DebugGraph();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Graph Info", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            var blocks = asset.GetBlocks();
            EditorGUILayout.IntField("Block Count", blocks?.Count ?? 0);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Double-click this asset or use the button above to open the Visual Graph Editor.",
                MessageType.Info);
        }

        // ── Double-click handler ─────────────────────────────────────────────────
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as GraphAsset;
            if (asset == null) return false;

            GraphEditorWindow.Open(asset);
            return true; // Consume the open event (prevents default text editor)
        }

        private void DebugGraph()
        {
            GraphAsset graph = (GraphAsset)target;
            if (graph == null) return;

            string graphInfo = graph.name + '\n';
            graphInfo += "Blocks: " + '\n';
            graphInfo += "Blocks Count: " + graph.blocks.Count.ToString() + '\n';

            int i = 0;
            foreach (var block in graph.blocks)
            {
                graphInfo += '\n';
                graphInfo += "  " + "Block" + i.ToString() + ": " + block.GetType().Name + '\n';
                graphInfo += "  " + "Ports:" + '\n';
                graphInfo += "  " + "Ports Count: " + block.Ports.Count.ToString() + '\n';

                int ii = 0;
                foreach (var port in block.Ports)
                {
                    graphInfo += '\n';
                    graphInfo += "      " + "Port" + ii.ToString() + ": " + port.name + '\n' + "          Connected Blocks:" + '\n';
                    foreach(var connectedBlock in port.ConnectedBlocks)
                    {
                        graphInfo += "                  " + connectedBlock.GetType().Name + '\n';
                    }
                    ii++;
                }
                i++;
            }

            Debug.Log(graphInfo);
        }
    }
}
