using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A serializable, node-based control-flow graph stored as a project asset.
///
/// Create one via <c>Assets ▸ Create ▸ Graphs ▸ Graph Asset</c>, then double-click
/// it (or press "Open Graph Editor" in the inspector) to edit it visually.
///
/// Every block is serialized through Unity's managed-reference system, so any
/// <c>[Serializable]</c> type — custom classes included — round-trips, and
/// <c>[SerializeReference]</c> fields stay polymorphic.
///
/// To make a specialized graph type (e.g. one that always starts with a particular
/// set of blocks), derive from this class, override <see cref="CreateDefaultBlocks"/>
/// and give the subclass its own <c>[CreateAssetMenu]</c> — everything else (the
/// editor window, auto-save, inspector button) keeps working with no further changes:
/// <code>
/// [CreateAssetMenu(fileName = "NewDialogueGraph", menuName = "Graphs/Dialogue Graph Asset")]
/// public class DialogueGraphAsset : GraphAsset
/// {
///     protected override IEnumerable&lt;GraphBlock&gt; CreateDefaultBlocks()
///     {
///         var entry = new EntryBlock { EditorPosition = new Vector2(80, 200) };
///         var start = new StartDialogueBlock { EditorPosition = new Vector2(340, 200) };
///         entry.Connections.Add(new GraphConnection("Out", start));
///         yield return entry;
///         yield return start;
///     }
/// }
/// </code>
/// </summary>
[CreateAssetMenu(fileName = "NewGraph", menuName = "Graph/Graph Asset")]
public class GraphAsset : ScriptableObject
{
    [SerializeReference] private List<GraphBlock> _blocks = new();
    [SerializeReference, HideInInspector] private GraphBlock _entry;

    /// <summary>Every block in the graph, connected or not.</summary>
    public List<GraphBlock> Blocks => _blocks;

    /// <summary>Block execution starts from. Always an <see cref="EntryBlock"/> in practice.</summary>
    public GraphBlock Entry
    {
        get => _entry;
        set => _entry = value;
    }

    /// <summary>Run the graph synchronously from its entry block.</summary>
    public void StartGraph()
    {
        GraphBlock start = _entry ?? _blocks.FirstOrDefault(b => b is EntryBlock);
        if (start == null)
        {
            Debug.LogWarning($"[GraphAsset] '{name}' has no entry block to start from.", this);
            return;
        }
        start.StartBlock();
    }

    /// <summary>
    /// Seeds <see cref="CreateDefaultBlocks"/> into an empty graph and (re)resolves
    /// <see cref="Entry"/>. Safe to call repeatedly — a no-op once the graph has blocks.
    /// Called automatically the first time the graph is opened in the editor.
    /// </summary>
    public void EnsureDefaultBlocks()
    {
        if (_blocks.Count == 0)
            _blocks.AddRange(CreateDefaultBlocks());

        _entry ??= _blocks.FirstOrDefault(b => b is EntryBlock);
    }

    /// <summary>
    /// Blocks a brand-new graph of this type starts with. The base implementation
    /// seeds a single <see cref="EntryBlock"/>; override to add more (and wire them
    /// together via <see cref="GraphBlock.Connections"/>) for a specialized graph type.
    /// </summary>
    protected virtual IEnumerable<GraphBlock> CreateDefaultBlocks()
    {
        yield return new EntryBlock { EditorPosition = new Vector2(120f, 200f) };
    }
}
