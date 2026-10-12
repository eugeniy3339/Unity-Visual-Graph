using GraphEditor;
using GraphEditor.CustomTools;
using System;
using UnityEngine;

public class ExcelDialogueImporterTool : CustomTool
{
    public static void ImportDialogue(ExcelTab<ExcelDialoguePart> dialogue)
    {
        GraphEditorWindow curGraphEditorWindow = GraphEditorWindow.Instance;
        if (curGraphEditorWindow == null) return;
        GraphAsset curOpenedGraph = curGraphEditorWindow.CurrentAsset;
        if (curOpenedGraph == null) return;
        SerializableGraphView curSerializableGraphView = curGraphEditorWindow.View;
        if (curSerializableGraphView == null) return;

        foreach(var dialoguePart in dialogue.classes)
        {
            GraphBlock block = CreateBlock(typeof(StartDialogueBlock));
            if (block == null) continue;

            StartDialogueBlock curBlock = block as StartDialogueBlock;
            curBlock.dialogue = new eugeniy3339.Dialogue.Dialogue { text = dialoguePart.dialogueText };
            curBlock.dialogue.options.Add(new eugeniy3339.Dialogue.Dialogue.DialogueOption { text = "..." });
        }

        CreateBlock(typeof(EndDialogueBlock));

        curSerializableGraphView.Save();
        curSerializableGraphView.Rebuild();
    }

    private static GraphBlock CreateBlock(Type type)
    {
        GraphEditorWindow curGraphEditorWindow = GraphEditorWindow.Instance;
        if (curGraphEditorWindow == null) return null;
        GraphAsset curOpenedGraph = curGraphEditorWindow.CurrentAsset;
        if (curOpenedGraph == null) return null;
        SerializableGraphView curSerializableGraphView = curGraphEditorWindow.View;
        if (curSerializableGraphView == null) return null;

        GraphBlock lastBlock = curOpenedGraph.Blocks.Count > 0 ? curOpenedGraph.Blocks[curOpenedGraph.Blocks.Count - 1] : null;
        Vector2 pos = lastBlock != null ? new Vector2(lastBlock.EditorPosition.x + 300f, lastBlock.EditorPosition.y) : new Vector2(120f, 200f);
        GraphBlock curBlock = curSerializableGraphView.CreateBlockOnPos(type, pos);

        if(lastBlock != null)
        {
            if(lastBlock.OutputPorts.Count > 0)
                lastBlock.connections.Add(new GraphConnection { fromPort = lastBlock.OutputPorts[0], target = curBlock });
        }

        return curBlock;
    }
}
