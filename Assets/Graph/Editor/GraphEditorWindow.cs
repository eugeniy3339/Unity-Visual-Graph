using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Graph.Logic;

namespace Graph.Editor
{
    /// <summary>
    /// The main Visual Graph Editor window. 
    /// Opens when the user double-clicks a GraphAsset or clicks "Open Graph" in its Inspector.
    /// </summary>
    public class GraphEditorWindow : EditorWindow
    {
        // ── State ────────────────────────────────────────────────────────────────
        private GraphAsset _asset;
        private GraphViewCanvas _graphView;
        private Label _titleLabel;

        // ── Static open helpers ──────────────────────────────────────────────────
        public static void Open(GraphAsset asset)
        {
            if (asset == null) return;

            // Reuse an existing window that already shows this asset, or open a new one.
            var windows = Resources.FindObjectsOfTypeAll<GraphEditorWindow>();
            foreach (var w in windows)
            {
                if (w._asset == asset)
                {
                    w.Focus();
                    return;
                }
            }

            var window = CreateWindow<GraphEditorWindow>("Visual Graph Editor");
            window.minSize = new Vector2(800, 500);
            window.LoadAsset(asset);
            window.Show();
        }

        // ── Unity callbacks ──────────────────────────────────────────────────────
        private void OnEnable()
        {
            BuildUI();
            if (_asset != null) LoadAsset(_asset);
        }

        private void OnDisable()
        {
            SaveGraph();
        }

        // ── UI construction ──────────────────────────────────────────────────────
        private void BuildUI()
        {
            // Top toolbar
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f));
            toolbar.style.height = 24;
            toolbar.style.paddingLeft = 6;
            toolbar.style.paddingRight = 6;
            toolbar.style.alignItems = Align.Center;

            _titleLabel = new Label("No Graph Loaded");
            _titleLabel.style.color = new StyleColor(Color.gray);
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.style.flexGrow = 1;
            toolbar.Add(_titleLabel);

            var saveButton = new Button(SaveGraph) { text = "Save" };
            saveButton.style.width = 60;
            toolbar.Add(saveButton);

            var pingButton = new Button(PingAsset) { text = "Ping" };
            pingButton.style.width = 60;
            toolbar.Add(pingButton);

            rootVisualElement.Add(toolbar);

            // Graph canvas placeholder — replaced when an asset is loaded
            var placeholder = new Label("Double-click a GraphAsset to open it here.")
            {
                name = "placeholder"
            };
            placeholder.style.position = Position.Absolute;
            placeholder.style.top = new Length(50, LengthUnit.Percent);
            placeholder.style.left = new Length(50, LengthUnit.Percent);
            placeholder.style.color = new StyleColor(Color.gray);
            rootVisualElement.Add(placeholder);
        }

        // ── Asset loading ────────────────────────────────────────────────────────
        private void LoadAsset(GraphAsset asset)
        {
            _asset = asset;
            titleContent = new GUIContent(asset.name, EditorGUIUtility.IconContent("d_BlendTree Icon").image);

            // Update title bar label
            if (_titleLabel != null)
                _titleLabel.text = asset.name;

            // Remove placeholder / previous graph view
            var old = rootVisualElement.Q("placeholder");
            old?.RemoveFromHierarchy();
            _graphView?.RemoveFromHierarchy();

            // Create the new graph view
            _graphView = new GraphViewCanvas(asset, this);
            _graphView.style.flexGrow = 1;
            rootVisualElement.Add(_graphView);
        }

        // ── Persistence ──────────────────────────────────────────────────────────
        public void SaveGraph()
        {
            if (_asset == null) return;
            _graphView?.CommitPositions();
            _asset.SaveGraph();
        }

        private void PingAsset()
        {
            if (_asset != null) EditorGUIUtility.PingObject(_asset);
        }
    }
}
