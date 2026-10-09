using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphEditor
{
    /// <summary>
    /// Node editor for <see cref="GraphAsset"/>. Opens on double-click of the asset,
    /// from the "Open Graph Editor" inspector button, or by selecting a graph in the toolbar.
    /// If a graph is already open, opening another one saves the current graph and swaps to the new one.
    /// </summary>
    public sealed class GraphEditorWindow : EditorWindow
    {
        [SerializeField] private GraphAsset _asset;

        private SerializableGraphView _view;
        private ObjectField _assetField;

        public static void Open(GraphAsset asset)
        {
            var window = GetWindow<GraphEditorWindow>("Graph");
            window.BuildIfNeeded();
            window.SetAsset(asset);
            window.Focus();
        }

        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is GraphAsset asset)
            {
                Open(asset);
                return true;
            }
            return false;
        }

        private void CreateGUI() => BuildIfNeeded();

        public void SetAsset(GraphAsset asset)
        {
            if (_asset == asset && _view != null && _view.Asset == asset)
                return;

            // Save the currently loaded graph before swapping
            if (_view != null && _asset != null)
                _view.Save();

            _asset = asset;
            titleContent = new GUIContent(asset != null ? asset.name : "Graph");

            if (_assetField != null && _assetField.value != asset)
                _assetField.SetValueWithoutNotify(asset);

            if (_view != null)
            {
                _view.Load(asset);
                if (asset != null)
                    _view.schedule.Execute(() => _view.FrameAll()).ExecuteLater(50);
            }
        }

        private void BuildIfNeeded()
        {
            if (_view != null)
                return;

            var toolbar = new Toolbar();

            _assetField = new ObjectField
            {
                objectType = typeof(GraphAsset),
                value = _asset,
                allowSceneObjects = false,
                tooltip = "Active Graph Asset. Drop or pick another graph to switch.",
                style = { width = 200 }
            };
            _assetField.RegisterValueChangedCallback(evt =>
            {
                SetAsset(evt.newValue as GraphAsset);
            });
            toolbar.Add(_assetField);

            toolbar.Add(new ToolbarButton(() => _view?.Save()) { text = "Save" });
            toolbar.Add(new ToolbarButton(() => _view?.FrameAll()) { text = "Frame All" });
            toolbar.Add(new ToolbarButton(() => _view?.RefreshPortsNow()) { text = "Refresh Ports" });
            toolbar.Add(new ToolbarButton(() => _view?.Rebuild()) { text = "Reload" });
            rootVisualElement.Add(toolbar);

            _view = new SerializableGraphView(this) { style = { flexGrow = 1 } };
            rootVisualElement.Add(_view);

            if (_asset != null)
            {
                _view.Load(_asset);
                _view.schedule.Execute(() => _view.FrameAll()).ExecuteLater(50);
            }
        }

        private void OnEnable()
        {
            // Survive domain reloads / window docking.
            BuildIfNeeded();
            if (_asset != null)
            {
                _view?.Load(_asset);
            }
        }

        private void OnDisable() => _view?.Save();
    }
}
