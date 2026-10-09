using UnityEngine;

/// <summary>
/// Drops a <see cref="GraphAsset"/> onto a GameObject and runs it.
/// </summary>
public class GraphRunner : MonoBehaviour
{
    [SerializeField] private GraphAsset _graph;
    [SerializeField] private bool _runOnStart = true;

    public GraphAsset Graph
    {
        get => _graph;
        set => _graph = value;
    }

    private void Start()
    {
        if (_runOnStart)
            Run();
    }

    [ContextMenu("Run Graph")]
    public void Run()
    {
        if (_graph == null)
        {
            Debug.LogWarning("[GraphRunner] No graph assigned.", this);
            return;
        }
        _graph.StartGraph();
    }
}
