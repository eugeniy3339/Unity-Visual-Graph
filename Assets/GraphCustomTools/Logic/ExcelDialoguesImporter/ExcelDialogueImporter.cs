using DialogueSystem.Logic;
using Graph.Logic;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace GraphCustomTools.Logic
{
    public static class ExcelDialogueImporter
    {
        public static void ImportDialogueToTheGraph(ExcelTab<ExcelDialoguePart> dialogueTab, GraphAsset graphAsset)
        {
            foreach(var dialoguePart in dialogueTab.classes)
            {
                StartDialogueBlock curBlock = AddNextBlock(typeof(StartDialogueBlock), graphAsset) as StartDialogueBlock;
                curBlock.dialogue = new Dialogue(dialoguePart.dialogueText, new List<Dialogue.DialogueOption> { new Dialogue.DialogueOption("...") });
            }

            AddNextBlock(typeof(EndDialogueBlock), graphAsset);
        }

        private static GraphBlock AddNextBlock(Type blockType, GraphAsset graphAsset)
        {
            GraphBlock lastBlock = graphAsset.blocks.Count > 0 ? graphAsset.blocks[graphAsset.blocks.Count - 1] : null;
            Vector2 blockPos = lastBlock != null ? new Vector2(lastBlock.position.x + 200f, lastBlock.position.y) : Vector2.zero;

            GraphBlock newBlock = GraphBlock.CreateBlock(blockType, blockPos);
            if (newBlock == null) return null;
            graphAsset.AddBlock(newBlock);

            if (lastBlock != null && lastBlock.Ports.Count > 0)
            {
                lastBlock.Ports[lastBlock.Ports.Count - 1].ConnectBlock(newBlock);
            }

            return newBlock;
        }
    }
}