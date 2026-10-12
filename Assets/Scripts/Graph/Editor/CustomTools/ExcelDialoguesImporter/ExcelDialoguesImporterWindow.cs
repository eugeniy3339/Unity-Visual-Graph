using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphEditor.CustomTools
{
    [CustomToolMenu("Excel Dialogue Importer")]
    public class ExcelDialoguesImporterWindow : CustomToolWindow
    {
        private Dialogues _dialoguesAsset;
        private int _selectedTabIndex = -1;

        private ObjectField _dialoguesField;
        private ScrollView _tabsScrollView;
        private Label _selectedTabLabel;
        private Button _importButton;

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;

            // 1. Header
            var titleLabel = new Label("Excel Dialogues Importer")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 4
                }
            };
            root.Add(titleLabel);

            var descriptionLabel = new Label("Select a Dialogues asset to view its tabs and choose which one to import.")
            {
                style =
                {
                    fontSize = 11,
                    color = new StyleColor(new Color(0.7f, 0.7f, 0.7f)),
                    marginBottom = 12
                }
            };
            root.Add(descriptionLabel);

            // 2. Dialogues Asset ObjectField
            _dialoguesField = new ObjectField("Dialogues Asset")
            {
                objectType = typeof(Dialogues),
                value = _dialoguesAsset,
                allowSceneObjects = false,
                tooltip = "Assign a Dialogues ScriptableObject asset containing Excel tabs."
            };
            _dialoguesField.style.marginBottom = 10;
            _dialoguesField.RegisterValueChangedCallback(evt =>
            {
                _dialoguesAsset = evt.newValue as Dialogues;
                _selectedTabIndex = -1;
                RefreshTabsList();
            });
            root.Add(_dialoguesField);

            // 3. Tabs Section Header
            var tabsHeaderLabel = new Label("Available Tabs:")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 4
                }
            };
            root.Add(tabsHeaderLabel);

            // 4. ScrollView for Tabs
            _tabsScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1,
                    minHeight = 160,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f)),
                    borderBottomColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f)),
                    borderLeftColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f)),
                    borderRightColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f)),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    backgroundColor = new StyleColor(new Color(0.16f, 0.16f, 0.16f)),
                    paddingTop = 4,
                    paddingBottom = 4,
                    paddingLeft = 4,
                    paddingRight = 4,
                    marginBottom = 10
                }
            };
            root.Add(_tabsScrollView);

            // 5. Selected Tab Status Label
            _selectedTabLabel = new Label("No tab selected.")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Italic,
                    color = new StyleColor(new Color(0.8f, 0.8f, 0.8f)),
                    marginBottom = 10
                }
            };
            root.Add(_selectedTabLabel);

            // 6. Import Button
            _importButton = new Button(OnImportClicked)
            {
                text = "Import",
                style =
                {
                    height = 32,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            root.Add(_importButton);

            // Initial population
            RefreshTabsList();
        }

        private void RefreshTabsList()
        {
            _tabsScrollView.Clear();

            if (_dialoguesAsset == null)
            {
                var helpBox = new HelpBox("Assign a Dialogues asset above to view its tabs.", HelpBoxMessageType.Info);
                _tabsScrollView.Add(helpBox);
                UpdateSelectedTabUI();
                return;
            }

            var tabs = _dialoguesAsset.tabs;
            if (tabs == null || tabs.Count == 0)
            {
                var helpBox = new HelpBox("No tabs found in the assigned Dialogues asset.", HelpBoxMessageType.Warning);
                _tabsScrollView.Add(helpBox);
                UpdateSelectedTabUI();
                return;
            }

            for (int i = 0; i < tabs.Count; i++)
            {
                int index = i;
                var tab = tabs[i];
                string tabName = string.IsNullOrEmpty(tab.name) ? $"Tab {index + 1}" : tab.name;
                int count = tab.classes != null ? tab.classes.Count : 0;

                bool isSelected = (_selectedTabIndex == index);

                var tabButton = new Button(() => SelectTab(index))
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        justifyContent = Justify.SpaceBetween,
                        alignItems = Align.Center,
                        paddingTop = 8,
                        paddingBottom = 8,
                        paddingLeft = 10,
                        paddingRight = 10,
                        marginBottom = 4,
                        borderTopLeftRadius = 4,
                        borderTopRightRadius = 4,
                        borderBottomLeftRadius = 4,
                        borderBottomRightRadius = 4,
                        borderTopWidth = 1,
                        borderBottomWidth = 1,
                        borderLeftWidth = 1,
                        borderRightWidth = 1,
                        borderTopColor = isSelected ? new StyleColor(new Color(0.24f, 0.5f, 0.88f)) : new StyleColor(new Color(0.25f, 0.25f, 0.25f)),
                        borderBottomColor = isSelected ? new StyleColor(new Color(0.24f, 0.5f, 0.88f)) : new StyleColor(new Color(0.25f, 0.25f, 0.25f)),
                        borderLeftColor = isSelected ? new StyleColor(new Color(0.24f, 0.5f, 0.88f)) : new StyleColor(new Color(0.25f, 0.25f, 0.25f)),
                        borderRightColor = isSelected ? new StyleColor(new Color(0.24f, 0.5f, 0.88f)) : new StyleColor(new Color(0.25f, 0.25f, 0.25f)),
                        backgroundColor = isSelected ? new StyleColor(new Color(0.2f, 0.35f, 0.55f)) : new StyleColor(new Color(0.22f, 0.22f, 0.22f))
                    }
                };

                var nameLabel = new Label(tabName)
                {
                    style =
                    {
                        unityFontStyleAndWeight = isSelected ? FontStyle.Bold : FontStyle.Normal,
                        color = isSelected ? new StyleColor(Color.white) : new StyleColor(new Color(0.9f, 0.9f, 0.9f))
                    }
                };
                tabButton.Add(nameLabel);

                var countLabel = new Label($"({count} entries)")
                {
                    style =
                    {
                        fontSize = 11,
                        color = isSelected ? new StyleColor(new Color(0.85f, 0.85f, 0.85f)) : new StyleColor(new Color(0.6f, 0.6f, 0.6f))
                    }
                };
                tabButton.Add(countLabel);

                _tabsScrollView.Add(tabButton);
            }

            UpdateSelectedTabUI();
        }

        private void SelectTab(int index)
        {
            _selectedTabIndex = index;
            RefreshTabsList();
        }

        private void UpdateSelectedTabUI()
        {
            if (_dialoguesAsset == null || _dialoguesAsset.tabs == null || _dialoguesAsset.tabs.Count == 0)
            {
                _selectedTabLabel.text = "No tab selected.";
                _selectedTabLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                return;
            }

            if (_selectedTabIndex >= 0 && _selectedTabIndex < _dialoguesAsset.tabs.Count)
            {
                var tab = _dialoguesAsset.tabs[_selectedTabIndex];
                int count = tab.classes != null ? tab.classes.Count : 0;
                _selectedTabLabel.text = $"Selected Tab: {tab.name} ({count} dialogue entries)";
                _selectedTabLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                _selectedTabLabel.text = "Click a tab above to select it.";
                _selectedTabLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            }
        }

        private void OnImportClicked()
        {
            if (_dialoguesAsset == null) return;
            if (_selectedTabIndex == -1) return;
            ExcelDialogueImporterTool.ImportDialogue(_dialoguesAsset.tabs[_selectedTabIndex]);
            try
            {
                
            }
            catch { }
        }
    }
}
