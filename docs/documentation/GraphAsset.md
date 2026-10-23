# Graph Asset

[← Back to General](General.md) | [Next: Graph Blocks →](GraphBlocks.md)

A **Graph Asset** is a `ScriptableObject` that stores the whole graph and all of its [blocks](GraphBlocks.md).

## Creating a Graph

1. Right-click in the **Project** window.
2. Select **Create → Graphs → Create new Graph Asset**.

## Opening the Graph Editor

Do one of the following:

- Double-click the Graph Asset.
- Select the Graph Asset and click **Open Graph** in the **Inspector** window.

## `GraphAsset` Script

### Public members

#### `StartGraph()`

Finds the first block of the graph and calls its [`StartBlock()`](GraphBlocks.md#startblock) function.

```csharp
graphAsset.StartGraph();
```

#### `Blocks`

A public `List<GraphBlock>` that contains **all** the blocks of the graph.
