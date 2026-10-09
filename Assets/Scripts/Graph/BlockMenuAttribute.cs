using System;

/// <summary>
/// Places a <see cref="GraphBlock"/> subclass in the graph editor's "Add Block"
/// search window. <c>[BlockMenu("Flow/Branch")]</c> nests it under a "Flow" group.
/// Blocks without this attribute land under "Misc".
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class BlockMenuAttribute : Attribute
{
    public string Path { get; }

    public BlockMenuAttribute(string path) => Path = path;
}
