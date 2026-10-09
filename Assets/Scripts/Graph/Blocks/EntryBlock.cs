using System;

/// <summary>
/// The single starting point of a graph. Created automatically with every new
/// <see cref="GraphAsset"/> and cannot be deleted in the editor.
/// </summary>
[Serializable]
public class EntryBlock : GraphBlock
{
    public override string DisplayName => "Entry";

    public override bool HasInputPort => false;
}
