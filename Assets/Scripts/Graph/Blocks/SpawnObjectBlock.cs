using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Instantiates a prefab and publishes it on a value output, so any block wired to
/// that pin can read the exact instance this block created.
/// </summary>
[Serializable]
[BlockMenu("Object/Spawn Object")]
public class SpawnObjectBlock : GraphBlock
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private Vector3 _position;

    private static readonly GraphPort[] Outputs = { new("Instance", typeof(GameObject)) };

    public override string DisplayName => "Spawn Object";
    public override IReadOnlyList<GraphPort> ValueOutputs => Outputs;

    public override void StartBlock()
    {
        GameObject instance = _prefab != null
            ? UnityEngine.Object.Instantiate(_prefab, _position, Quaternion.identity)
            : null;

        SetOutput("Instance", instance);
        Fire();
    }
}
