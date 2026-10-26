# Unity Visual Graph

A simple visual graph for Unity that can start **custom blocks**.

Create a graph asset, build it in the Graph Editor, and write your own blocks in C# by inheriting from `GraphBlock`.

## Documentation

The documentation is in the [`documentation`](documentation/) folder, or you can download it from the documentation release.

| Document | Description |
| --- | --- |
| [General](documentation/General.md) | Overview of the graph system |
| [Graph Asset](documentation/GraphAsset.md) | Creating, opening and starting a graph |
| [Graph Blocks](documentation/GraphBlocks.md) | Creating custom blocks, ports, values and execution flow |

## Quick start

1. Right-click in the **Project** window and select **Create → Graphs → Create new Graph Asset**.
2. Double-click the asset to open the Graph Editor.
3. Create your own block:

```csharp
[Serializable]
[BlockMenu("Custom/TestBlock")]
public class TestBlock : GraphBlock
{
    public override void StartBlock()
    {
        Debug.Log("Hello from TestBlock!");
        Fire(0);
    }
}
```

4. Right-click the graph background, select **Create Node**, and pick your block.
5. Start the graph from code:

```csharp
graphAsset.StartGraph();
```

See [Graph Blocks](docs/GraphBlocks.md) for ports, input/output values and more.

## License

This project is licensed under the [MIT License](https://en.wikipedia.org/wiki/MIT_License). You are free to modify and share it.

## Credits

- [**eugeniy3339**](https://github.com/eugeniy3339/Unity-Visual-Graph) – Unity Visual Graph
- [**mikito**](https://github.com/mikito/unity-excel-importer) – Excel importer for Unity
