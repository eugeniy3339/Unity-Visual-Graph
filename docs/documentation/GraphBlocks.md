# Graph Blocks

[← Back to General](General.md) | [← Graph Asset](GraphAsset.md)

Blocks are the nodes of a graph. Every block inherits from the `GraphBlock` class and lives inside a [Graph Asset](GraphAsset.md).

## Contents

- [Creating a new block](#creating-a-new-block)
- [Block script](#block-script)
- [Public members](#public-members)
  - [`DisplayName`](#displayname) · [`HasInputPort`](#hasinputport) · [`Ports`](#ports) · [`ValueOutputs`](#valueoutputs) · [`ValueInputs`](#valueinputs)
  - [`StartBlock`](#startblock) · [`GetInputValue`](#getinputvalue) · [`GetOutputValue`](#getoutputvalue) · [`SetOutputValue`](#setoutputvalue) · [`Fire`](#fire) · [`CreateBlock`](#createblock)

---

## Creating a new block

### 1. Create a block script

Either:

- Right-click in the **Project** window and select **Create → Graphs → New Block Graph Script**, or
- Create a new class that inherits from `GraphBlock`.

### 2. Add the block to a graph

1. Right-click the graph's background.
2. Select **Create Node**.
3. Select the block you want.

---

## Block script

> [!IMPORTANT]
> The block class **must** have the `[Serializable]` attribute, otherwise it will not be saved.

To configure the block's **group** and **name** in the *Create Node* menu, add the `[BlockMenu]` attribute.

**Example:**

```csharp
[Serializable]
[BlockMenu("Custom/TestBlock")]
public class TestBlock : GraphBlock
{

}
```

---

## Public members

### `DisplayName`

Stores the block's display name. Can be **overridden**.

### `HasInputPort`

Stores whether the block has an input port. Can be **overridden**.

### `Ports`

A read-only list of `GraphOutputPort`. Can be **overridden**.

> [!NOTE]
> The field that `Ports` returns must have `[SerializeReference]` or `[SerializeField]`, so the ports can be saved. The same applies to [`ValueOutputs`](#valueoutputs) and [`ValueInputs`](#valueinputs).

**Example:**

```csharp
[SerializeReference, HideInInspector]
protected GraphOutputPort[] _ports = { new GraphOutputPort("Out") };

public virtual IReadOnlyList<GraphOutputPort> Ports => _ports;
```

### `ValueOutputs`

A read-only list of `GraphValueOutputPort`. Can be **overridden**. The returned field must have `[SerializeReference]` or `[SerializeField]`.

**Example:**

```csharp
[SerializeReference, HideInInspector]
protected GraphValueOutputPort[] _valueOutputs = { };

public virtual IReadOnlyList<GraphValueOutputPort> ValueOutputs => _valueOutputs;
```

### `ValueInputs`

A read-only list of `GraphValueInputPort`. Can be **overridden**. The returned field must have `[SerializeReference]` or `[SerializeField]`.

**Example:**

```csharp
[SerializeReference, HideInInspector]
protected GraphValueInputPort[] _valueInputs = { };

public virtual IReadOnlyList<GraphValueInputPort> ValueInputs => _valueInputs;
```

### `StartBlock`

Called when the block should start executing. It is called:

- by the [`GraphAsset`](GraphAsset.md#startgraph) on the **first block** of the graph when `StartGraph()` is called, or
- by **other blocks**, when their [`Fire`](#fire) function is called with a port that is connected to this block.

Can be **overridden**. By default it calls `Fire(0)`.

### `GetInputValue`

Takes a **port name** or the **port itself** and returns the value of the input port.

**Example:**

```csharp
[SerializeReference, HideInInspector]
private GraphValueInputPort[] _inputPorts = { new GraphValueInputPort("TestInput", typeof(string)) };

public override IReadOnlyList<GraphValueInputPort> ValueInputs => _inputPorts;

public override void StartBlock()
{
    string value = GetInputValue<string>("TestInput");
}
```

### `GetOutputValue`

Takes a **port name** or the **port itself** and returns the value of the output port.

**Example:**

```csharp
[SerializeReference, HideInInspector]
private GraphValueOutputPort[] _outputPorts = { new GraphValueOutputPort("TestOutput", typeof(string)) };

public override IReadOnlyList<GraphValueOutputPort> ValueOutputs => _outputPorts;

public override void StartBlock()
{
    string value = GetOutputValue<string>("TestOutput");
}
```

### `SetOutputValue`

Takes a **port name** (or the **port itself**) and the **value** you want to set, and sets the value of the output port.

**Example:**

```csharp
[SerializeReference, HideInInspector]
private GraphValueOutputPort[] _outputPorts = { new GraphValueOutputPort("TestOutput", typeof(string)) };

public override IReadOnlyList<GraphValueOutputPort> ValueOutputs => _outputPorts;

public override void StartBlock()
{
    SetOutputValue("TestOutput", "Test");
}
```

### `CreateBlock`

A **static** function that creates a block of the given type and returns it.

**Example:**

```csharp
GraphBlock graphBlock = GraphBlock.CreateBlock<GraphBlock>("New Block");
```

---

## Protected members

### `Fire`

Takes a **port index** or the **port itself** and calls the [`StartBlock`](#startblock) functions of the blocks connected to that port.

**Example:**

```csharp
public override void StartBlock()
{
    Fire(0); // Starts the blocks that are connected to the port with index 0
}
```

**Start order:**

![Blocks start queue](images/BlocksStartQueue.png)

*Blocks are started in the order shown above: the entry block fires three chains, each one is followed to its end before the next one starts.*