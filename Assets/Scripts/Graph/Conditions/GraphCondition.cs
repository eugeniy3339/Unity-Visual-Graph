using System;
using UnityEngine;

/// <summary>
/// Polymorphic condition used by branching blocks. Because block fields declared as
/// <c>[SerializeReference] GraphCondition</c> keep their concrete type, the graph
/// editor shows a type dropdown and edits the chosen condition inline — this is the
/// pattern to follow for any "pick a custom class" field.
/// </summary>
[Serializable]
public abstract class GraphCondition
{
    public abstract bool Evaluate();
}

/// <summary>Always returns a fixed value. Handy placeholder / test condition.</summary>
[Serializable]
public sealed class ConstantCondition : GraphCondition
{
    [SerializeField] private bool _value = true;

    public override bool Evaluate() => _value;
}
