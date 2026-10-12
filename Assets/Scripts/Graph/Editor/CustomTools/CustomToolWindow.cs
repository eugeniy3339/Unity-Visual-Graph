using GraphEditor;
using UnityEditor;

public class CustomToolWindow : EditorWindow
{
    /// <summary>
    /// Reference to the active <see cref="GraphEditorWindow"/> that launched this tool.
    /// </summary>
    public static GraphEditorWindow GraphEditor { get; internal set; }

    /// <summary>
    /// The active <see cref="GraphAsset"/> currently open in the editor window, if any.
    /// </summary>
    public static GraphAsset CurrentGraph => GraphEditor != null ? GraphEditor.CurrentAsset : null;

    /// <summary>
    /// Called when the tool is opened from the Graph Editor toolbar.
    /// </summary>
    /// <param name="graphEditor">The Graph Editor window instance.</param>

    public virtual void Open(GraphEditorWindow graphEditorWindow)
    {
        GraphEditor = graphEditorWindow;
    }
}
