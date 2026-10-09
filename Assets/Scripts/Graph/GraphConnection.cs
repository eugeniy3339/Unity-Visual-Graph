using System;
using UnityEngine;

/// <summary>
/// A single directed flow link between two <see cref="GraphBlock"/>s.
/// <see cref="target"/> is stored as a managed reference so it points at the very
/// same block instance held by <see cref="GraphAsset.Blocks"/> (Unity keeps shared
/// <c>[SerializeReference]</c> references intact).
/// </summary>
[Serializable]
public class GraphConnection
{
    /// <summary>Name of the output port on the source block this link leaves from.</summary>
    public string fromPort = "Out";

    [SerializeReference] public GraphBlock target;

    public GraphConnection() { }

    public GraphConnection(string fromPort, GraphBlock target)
    {
        this.fromPort = fromPort;
        this.target = target;
    }
}
