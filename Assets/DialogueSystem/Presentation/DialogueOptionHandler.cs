using DialogueSystem.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueSystem.Presentation
{
    public class DialogueOptionHandler : MonoBehaviour
    {
        public static DialogueOptionHandler SpawnDialogueOptionHandler(GameObject optionPrefab, Dialogue.DialogueOption dialogueOption, Transform optionParent)
        {
            GameObject optionObject = Instantiate(optionPrefab, optionParent);
            DialogueOptionHandler optionHandler = optionObject.GetComponentInChildren<DialogueOptionHandler>();
            optionHandler.SetUp(dialogueOption);
            return optionHandler;
        }

        private void SetUp(Dialogue.DialogueOption dialogueOption)
        {
            this.dialogueOption = dialogueOption;
            optionText.text = dialogueOption.text;
            optionButton.onClick.AddListener(OnClick);
        }

        private Dialogue.DialogueOption dialogueOption;
        private Button optionButton;
        private TMP_Text optionText;

        private void Awake()
        {
            optionButton = GetComponentInChildren<Button>();
            optionText = GetComponentInChildren<TMP_Text>();
        }

        private void OnClick()
        {
            dialogueOption.Select();
        }
    }
}