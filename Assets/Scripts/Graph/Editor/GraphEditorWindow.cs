using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphEditor
{
    /// <summary>
    /// Node editor for <see cref="GraphAsset"/>. Opens on double-click of the asset
    /// or from the "Open Graph Editor" inspector button.
    /// </summary>
    public sealed class GraphEditorWindow : EditorWindow
    {
        [SerializeField] private GraphAsset _asset;

        private SerializableGraphView _view;

        public static void Open(GraphAsset asset)
        {
            var window = GetWindow<GraphEditorWindow>("Graph");
            window.titleContent = new GUIContent(asset != null ? asset.name : "Graph");
            window.BuildIfNeeded();

            bool alreadyLoaded = window._asset == asset && window._view != null;
            window._asset = asset;
            if (!alreadyLoaded)
                window._view?.Load(asset);
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

        private void BuildIfNeeded()
        {
            if (_view != null)
                return;

            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(() => _view?.Save()) { text = "Save" });
            toolbar.Add(new ToolbarButton(() => _view?.FrameAll()) { text = "Frame All" });
            toolbar.Add(new ToolbarButton(() => _view?.RefreshPortsNow()) { text = "Refresh Ports" });
            toolbar.Add(new ToolbarButton(() => _view?.Rebuild()) { text = "Reload" });
            rootVisualElement.Add(toolbar);

            _view = new SerializableGraphView(this) { style = { flexGrow = 1 } };
            rootVisualElement.Add(_view);

            if (_asset != null)
                _view.Load(_asset);
        }

        private void OnEnable()
        {
            // Survive domain reloads / window docking.
            if (_asset != null)
            {
                BuildIfNeeded();
                _view?.Load(_asset);
            }
        }

        private void OnDisable() => _view?.Save();
    }
}
