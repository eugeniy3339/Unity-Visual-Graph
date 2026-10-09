using UnityEditor;
using UnityEngine;

namespace GraphEditor
{
    [CustomEditor(typeof(GraphAsset), editorForChildClasses: true)]
    public sealed class GraphAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Graph Editor", GUILayout.Height(28f)))
                GraphEditorWindow.Open((GraphAsset)target);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Raw data", EditorStyles.boldLabel);
            DrawDefaultInspector();
        }
    }
}
