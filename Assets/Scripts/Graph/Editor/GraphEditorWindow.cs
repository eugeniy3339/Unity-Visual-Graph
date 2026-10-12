using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using GraphEditor.CustomTools;
using System.Drawing.Printing;

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

        public static GraphEditorWindow Instance { get { return GetWindow<GraphEditorWindow>("Graph"); } }

        public GraphAsset CurrentAsset => _asset;

        private SerializableGraphView _view;
        public SerializableGraphView View => _view;

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
            var customToolsMenu = new ToolbarMenu { text = "Custom Tools" };
            PopulateCustomToolsMenu(customToolsMenu);
            toolbar.Add(customToolsMenu);
            rootVisualElement.Add(toolbar);

            _view = new SerializableGraphView(this) { style = { flexGrow = 1 } };
            rootVisualElement.Add(_view);

            if (_asset != null)
            {
                _view.Load(_asset);
                _view.schedule.Execute(() => _view.FrameAll()).ExecuteLater(50);
            }
        }

        private struct CustomToolEntry
        {
            public string Path;
            public Type Type;
        }

        private static List<CustomToolEntry> _cachedTools;

        private static List<CustomToolEntry> FindCustomTools()
        {
            if (_cachedTools != null)
                return _cachedTools;

            var results = new List<CustomToolEntry>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string asmName = assembly.GetName().Name;
                if (asmName.StartsWith("System") || asmName.StartsWith("mscorlib") || asmName.StartsWith("Unity.") || asmName.StartsWith("UnityEngine"))
                    continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }
                catch
                {
                    continue;
                }

                if (types == null) continue;

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract) continue;

                    var attr = type.GetCustomAttribute<CustomToolMenuAttribute>(false);
                    if (attr != null && !string.IsNullOrEmpty(attr.Path))
                    {
                        results.Add(new CustomToolEntry
                        {
                            Path = attr.Path,
                            Type = type
                        });
                    }
                }
            }

            results.Sort((a, b) => string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase));
            _cachedTools = results;
            return _cachedTools;
        }

        private void PopulateCustomToolsMenu(ToolbarMenu menu)
        {
            var tools = FindCustomTools();
            if (tools.Count == 0)
            {
                menu.menu.AppendAction("No Tools Found", null, DropdownMenuAction.Status.Disabled);
                return;
            }

            foreach (var tool in tools)
            {
                Type toolType = tool.Type;
                menu.menu.AppendAction(tool.Path, _ => OpenCustomTool(toolType));
            }
        }

        private void OpenCustomTool(Type toolType)
        {
            if (typeof(EditorWindow).IsAssignableFrom(toolType))
            {
                var window = GetWindow(toolType);
                if (window is CustomToolWindow customToolWindow)
                {
                    customToolWindow.Open(this);
                }
                window.Show();
                window.Focus();
            }
            else
            {
                var method = toolType.GetMethod("Execute", new[] { typeof(GraphEditorWindow) })
                          ?? toolType.GetMethod("Open", new[] { typeof(GraphEditorWindow) })
                          ?? toolType.GetMethod("Execute", Type.EmptyTypes)
                          ?? toolType.GetMethod("Open", Type.EmptyTypes);

                if (method != null)
                {
                    object instance = method.IsStatic ? null : Activator.CreateInstance(toolType);
                    if (method.GetParameters().Length == 1)
                        method.Invoke(instance, new object[] { this });
                    else
                        method.Invoke(instance, null);
                }
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
