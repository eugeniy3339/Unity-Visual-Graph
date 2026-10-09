using System;

/// <summary>
/// Declares one typed data (variable) pin on a <see cref="GraphBlock"/>. Used for
/// <see cref="GraphBlock.ValueInputs"/> / <see cref="GraphBlock.ValueOutputs"/> — the
/// pins that pass values (any type, custom classes included) between blocks, as
/// opposed to the execution pins that pass control flow.
/// </summary>
public readonly struct GraphPort
{
    public string Name { get; }
    public Type Type { get; }

    public GraphPort(string name, Type type)
    {
        Name = name;
        Type = type;
    }
}
