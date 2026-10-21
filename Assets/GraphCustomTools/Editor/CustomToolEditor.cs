using Graph.Logic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphCustomTools.Editor
{
    /// <summary>
    /// Base window for custom tool editors. A tool editor works on one <see cref="GraphAsset"/>
    /// (the graph it was opened for) and only has to provide its title and its content.
    /// Open a tool with <c>CustomToolEditor.Open&lt;TEditor&gt;(graph)</c>.
    /// </summary>
    public abstract class CustomToolEditor : EditorWindow
    {
        [SerializeField] private GraphAsset _targetGraph;

        /// <summary>The graph this tool was opened for.</summary>
        protected GraphAsset TargetGraph => _targetGraph;

        protected abstract string WindowTitle { get; }

        /// <summary>Fill the window. Called on open and after every domain reload.</summary>
        protected abstract void BuildContent(VisualElement root);

        public static T Open<T>(GraphAsset targetGraph) where T : CustomToolEditor
        {
            var window = GetWindow<T>(true);
            window.titleContent = new GUIContent(window.WindowTitle);
            window._targetGraph = targetGraph;
            window.Rebuild();
            window.Show();
            window.Focus();
            return window;
        }

        private void CreateGUI() => Rebuild();

        private void Rebuild()
        {
            rootVisualElement.Clear();
            BuildContent(rootVisualElement);
        }
    }
}
