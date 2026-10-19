using Graph.Logic;
using UnityEditor;
using UnityEngine;

namespace Graph.Editor
{
    [CustomEditor(typeof(GraphAsset), editorForChildClasses: true)]
    public sealed class GraphAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Graph Editor", GUILayout.Height(28f)))
                //GraphEditorWindow.Open((GraphAsset)target);
                Debug.Log("Open Editor Button was pressed");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Raw data", EditorStyles.boldLabel);
            DrawDefaultInspector();
        }
    }
}
