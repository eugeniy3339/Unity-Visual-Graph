using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reads a GameObject from a value input (e.g. wired to a <see cref="SpawnObjectBlock"/>'s
/// "Instance" output) and destroys it. Demonstrates consuming a variable produced by
/// another block.
/// </summary>
[Serializable]
[BlockMenu("Object/Destroy Object")]
public class DestroyObjectBlock : GraphBlock
{
    private static readonly GraphPort[] Inputs = { new("Target", typeof(GameObject)) };

    public override string DisplayName => "Destroy Object"; 
    public override IReadOnlyList<GraphPort> ValueInputs => Inputs;

    public override void StartBlock()
    {
        GameObject target = GetInput<GameObject>("Target");
        if (target != null)
            UnityEngine.Object.Destroy(target);

        Fire();
    }
}
