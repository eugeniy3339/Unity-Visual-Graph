using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for every node in a <see cref="GraphAsset"/>.
///
/// To add a new block type: derive from this class, mark it <c>[Serializable]</c>,
/// give it a <c>[BlockMenu("Category/Name")]</c> attribute and add whatever
/// <c>[SerializeField]</c> / <c>[SerializeReference]</c> fields you need. They show
/// up in the graph editor automatically (including polymorphic custom classes via
/// <c>[SerializeReference]</c>).
/// </summary>
[Serializable]
public abstract class GraphBlock
{
    [SerializeField, HideInInspector] private string _id;
    [SerializeField, HideInInspector] private Vector2 _editorPosition;

    // Outgoing flow connections. Authored by the editor, do not edit by hand.
    [SerializeField, HideInInspector] private List<GraphConnection> _connections = new();

    // Incoming value (variable) connections, keyed by this block's input port name.
    // Authored by the editor, do not edit by hand.
    [SerializeField, HideInInspector] private List<GraphValueConnection> _valueConnections = new();

    // Values this block produced during the current run. Not serialized: it is
    // execution-time state, recomputed every time the block runs.
    private readonly Dictionary<string, object> _outputValues = new();

    /// <summary>Stable identifier, generated lazily on first access.</summary>
    public string Id
    {
        get
        {
            if (string.IsNullOrEmpty(_id))
                _id = Guid.NewGuid().ToString("N");
            return _id;
        }
    }

    /// <summary>Node position inside the graph editor. Editor-only data.</summary>
    public Vector2 EditorPosition
    {
        get => _editorPosition;
        set => _editorPosition = value;
    }

    /// <summary>Outgoing connections keyed by source port name.</summary>
    public List<GraphConnection> Connections => _connections;

    /// <summary>Incoming value connections keyed by this block's input port name.</summary>
    public List<GraphValueConnection> ValueConnections => _valueConnections;

    /// <summary>Title shown on the node header. Override to customize.</summary>
    public virtual string DisplayName => GetType().Name;

    /// <summary>
    /// Names of the output ports this block exposes. One connection port each,
    /// every port can fan out to many blocks. Override for branching blocks.
    /// </summary>
    public virtual IReadOnlyList<string> OutputPorts => DefaultOutput;
    private static readonly string[] DefaultOutput = { "Out" };

    /// <summary>Whether this block accepts an incoming flow connection.</summary>
    public virtual bool HasInputPort => true;

    /// <summary>
    /// Typed variable pins this block reads from other blocks. Override to declare
    /// inputs, then read them from <see cref="StartBlock"/> with <see cref="GetInput{T}"/>.
    /// </summary>
    public virtual IReadOnlyList<GraphPort> ValueInputs => Array.Empty<GraphPort>();

    /// <summary>
    /// Typed variable pins this block produces for other blocks to read. Override to
    /// declare outputs, then publish them from <see cref="StartBlock"/> with <see cref="SetOutput"/>.
    /// </summary>
    public virtual IReadOnlyList<GraphPort> ValueOutputs => Array.Empty<GraphPort>();

    /// <summary>Called when execution reaches this block.</summary>
    public virtual void StartBlock() => Fire();

    /// <summary>Continue execution along every connection leaving <paramref name="port"/>.</summary>
    protected void Fire(string port = "Out")
    {
        for (int i = 0; i < _connections.Count; i++)
        {
            GraphConnection connection = _connections[i];
            if (connection != null && connection.fromPort == port)
                connection.target?.StartBlock();
        }
    }

    /// <summary>
    /// Publishes a value on one of this block's <see cref="ValueOutputs"/> so blocks
    /// wired to it can read it with <see cref="GetInput{T}"/>. Call this before
    /// <see cref="Fire"/> reaches a consumer (i.e. produce the value, then continue flow).
    /// </summary>
    public void SetOutput(string portName, object value) => _outputValues[portName] = value;

    /// <summary>
    /// Reads a value this block previously published via <see cref="SetOutput"/>.
    /// Used by a connected block's <see cref="GetInput{T}"/> — call directly only when
    /// reaching into another block manually.
    /// </summary>
    public T GetOutputValue<T>(string portName, T fallback = default)
    {
        return _outputValues.TryGetValue(portName, out object value) && value is T typed ? typed : fallback;
    }

    /// <summary>
    /// Reads the value wired into one of this block's <see cref="ValueInputs"/>, i.e.
    /// whatever the connected block last published on the matching output. Returns
    /// <paramref name="fallback"/> if nothing is connected, or the source hasn't run yet.
    /// </summary>
    protected T GetInput<T>(string portName, T fallback = default)
    {
        GraphValueConnection wire = _valueConnections.Find(c => c != null && c.inputPort == portName);
        return wire?.source != null ? wire.source.GetOutputValue(wire.fromPort, fallback) : fallback;
    }
}
