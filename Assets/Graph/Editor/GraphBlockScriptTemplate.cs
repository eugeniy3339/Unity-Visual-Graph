using UnityEngine;

namespace Graph.Editor
{
    using UnityEditor;

    public static class GraphBlockScriptTemplate
    {
        private const string TemplatePath = "Assets/Graph/Editor/GraphBlockTemplate.cs.txt";

        [MenuItem("Assets/Create/Graphs/New Graph Block Script", priority = 80)]
        public static void CreateBlockScript()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
                TemplatePath,
                "NewGraphBlock.cs"
            );
        }
    }
}
