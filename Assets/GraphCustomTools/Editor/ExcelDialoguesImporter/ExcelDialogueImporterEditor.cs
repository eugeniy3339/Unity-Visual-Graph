using Graph.Logic;
using GraphCustomTools.Logic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Graph.Editor;

namespace GraphCustomTools.Editor 
{
    /// <summary>
    /// Editor for <see cref="ExcelDialogueImporter"/>: pick a <see cref="Dialogues"/> asset,
    /// select one of its tabs and import that tab into the target graph.
    /// </summary>
    [CustomToolMenu("Import/Excel Dialogues")]
    public sealed class ExcelDialogueImporterEditor : CustomToolEditor
    {
        private static readonly Color SelectedColor = new Color(0.17f, 0.36f, 0.53f, 1f);

        [SerializeField] private Dialogues _dialogues;
        [SerializeField] private string _ningger;

        private int _selectedIndex = -1;
        private ScrollView _tabList;
        private Button _importButton;

        protected override string WindowTitle => "Excel Dialogue Importer";

        public static void Open(GraphAsset targetGraph) => Open<ExcelDialogueImporterEditor>(targetGraph);

        protected override void BuildContent(VisualElement root)
        {
            root.style.paddingLeft = 6;
            root.style.paddingRight = 6;
            root.style.paddingTop = 6;
            root.style.paddingBottom = 6;

            var dialoguesField = new ObjectField("Dialogues")
            {
                objectType = typeof(Dialogues),
                value = _dialogues,
                allowSceneObjects = false
            };
            dialoguesField.RegisterValueChangedCallback(evt =>
            {
                _dialogues = evt.newValue as Dialogues;
                _selectedIndex = -1;
                RefreshTabList();
            });
            root.Add(dialoguesField);

            root.Add(new Label("Tabs") { style = { marginTop = 6, unityFontStyleAndWeight = FontStyle.Bold } });

            _tabList = new ScrollView
            {
                style =
                {
                    flexGrow = 1,
                    marginTop = 2,
                    borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                    borderTopColor = Color.gray, borderBottomColor = Color.gray,
                    borderLeftColor = Color.gray, borderRightColor = Color.gray
                }
            };
            root.Add(_tabList);

            _importButton = new Button(Import) { text = "Import", style = { marginTop = 6, height = 24 } };
            root.Add(_importButton);

            RefreshTabList();
        }

        // Excel may have been re-imported while the window was open.
        private void OnFocus() => RefreshTabList();

        private void RefreshTabList()
        {
            if (_tabList == null)
                return;

            _tabList.Clear();

            var tabs = _dialogues != null ? _dialogues.tabs : null;
            if (tabs == null || tabs.Count == 0)
            {
                _selectedIndex = -1;
                _tabList.Add(new Label(_dialogues == null ? "Assign a Dialogues asset." : "No tabs found.")
                {
                    style = { paddingLeft = 4, paddingTop = 2, opacity = 0.6f }
                });
                UpdateImportButton();
                return;
            }

            if (_selectedIndex >= tabs.Count)
                _selectedIndex = -1;

            for (int i = 0; i < tabs.Count; i++)
            {
                int index = i;
                var tab = tabs[i];
                string tabName = tab != null && !string.IsNullOrEmpty(tab.name) ? tab.name : "(unnamed)";
                int count = tab?.classes?.Count ?? 0;

                var item = new Label($"{tabName}  ({count})")
                {
                    style = { paddingLeft = 4, paddingTop = 2, paddingBottom = 2 }
                };
                item.RegisterCallback<ClickEvent>(_ => SelectTab(index));
                _tabList.Add(item);
            }

            ApplySelection();
            UpdateImportButton();
        }

        private void SelectTab(int index)
        {
            _selectedIndex = index;
            ApplySelection();
            UpdateImportButton();
        }

        private void ApplySelection()
        {
            for (int i = 0; i < _tabList.childCount; i++)
            {
                _tabList[i].style.backgroundColor = i == _selectedIndex
                    ? new StyleColor(SelectedColor)
                    : new StyleColor(StyleKeyword.Null);
            }
        }

        private bool HasValidSelection =>
            _dialogues != null
            && _dialogues.tabs != null
            && _selectedIndex >= 0
            && _selectedIndex < _dialogues.tabs.Count
            && _dialogues.tabs[_selectedIndex] != null;

        private void UpdateImportButton()
        {
            // Needs a selected tab, and a graph to import into.
            _importButton?.SetEnabled(HasValidSelection && TargetGraph != null);
        }

        private void Import()
        {
            if (!HasValidSelection || TargetGraph == null)
                return;

            Undo.RecordObject(TargetGraph, "Import Excel Dialogue");
            ExcelDialogueImporter.ImportDialogueToTheGraph(_dialogues.tabs[_selectedIndex], TargetGraph);
            TargetGraph.SaveGraph();
            GraphEditorWindow.Instance?.View?.Rebuild();
        }
    }
}
