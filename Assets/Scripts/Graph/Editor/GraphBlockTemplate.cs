using UnityEditor;

public static class GraphBlockScriptTemplate
{
    private const string TemplatePath = "Assets/Scripts/Graph/Editor/GraphBlockTemplate.cs.txt";

    [MenuItem("Assets/Create/Graph/New Graph Block Script", priority = 80)]
    public static void CreateBlockScript()
    {
        ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
            TemplatePath,
            "NewGraphBlock.cs" // Default filename
        );
    }
}