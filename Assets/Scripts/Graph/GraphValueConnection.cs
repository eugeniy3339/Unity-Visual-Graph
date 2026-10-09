using System;
using UnityEngine;

/// <summary>
/// Wires one of this block's value inputs to another block's value output. Stored on
/// the consuming block since a value input accepts exactly one source.
/// </summary>
[Serializable]
public class GraphValueConnection
{
    /// <summary>Name of the value input port on the block that owns this connection.</summary>
    public string inputPort;

    /// <summary>Name of the value output port on <see cref="source"/> to read from.</summary>
    public string fromPort;

    [SerializeReference] public GraphBlock source;

    public GraphValueConnection() { }

    public GraphValueConnection(string inputPort, string fromPort, GraphBlock source)
    {
        this.inputPort = inputPort;
        this.fromPort = fromPort;
        this.source = source;
    }
}
