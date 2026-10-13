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
    }
}
