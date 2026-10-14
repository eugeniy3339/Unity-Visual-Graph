A simple visual programming graph for Unity that executes precoded blocks.

## How Execution Works

Execution begins at the **Entry** block.

Every block contains a `Fire()` method which triggers `StartBlock()` on child blocks.

By default, `StartBlock()` calls `Fire()` unless overridden.

Blocks start in a queue like this:

![Blocks start in a queue like this](images/BlocksStartQueue.png)

## Creating Custom Blocks

You can create new block scripts using two methods:

* **Template:** Go to `Create > Graph > Graph Block Script`.
* **Manual:** Inherit from `GraphBlock` and add the `[BlockMenu]` attribute.

### Example Block Script

```csharp
[BlockMenu("Custom/NewBlock")]
public class NewBlock : GraphBlock
{
}
```

## Handling Inputs & Outputs

Blocks can pass data between each other through ports.

### 1. Adding Inputs

Define an array of `GraphValueInputPort` objects and override `ValueInputs`:

```csharp
protected GraphValueInputPort[] valueInputs = { new GraphValueInputPort("StringInput", typeof(string)) };
public virtual IReadOnlyList<GraphValueInputPort> ValueInputs => valueInputs;
```

### 2. Adding Outputs

Override `ValueOutputs` using the same structure:

```csharp
protected GraphValueOutputPort[] valueOutputs = { new GraphValueOutputPort("StringOutput", typeof(string)) };
public virtual IReadOnlyList<GraphValueOutputPort> ValueOutputs => valueOutputs;
```

### 3. Getting & Setting Port Values

```csharp
// Get value from an input port
object value = GetInput("portName");
object value = GetInput(port);

// Set value on an output port
SetOutputValue("portName", value);
SetOutputValue(port, value);

// Get value from an output port
object outputValue = GetOutputValue("portName");
object outputValue = GetOutputValue(port);
```

### 4. Custom Output Executions

You can manage output flow ports by overriding `OutputPorts`:

```csharp
protected GraphOutputPort[] ports = { new GraphOutputPort("Out"), new GraphOutputPort("False"), new GraphOutputPort("True") }; 
public virtual IReadOnlyList<GraphOutputPort> Ports => ports;
```

To trigger execution on a specific output port other than the default (0), pass the port index, or the port it self to the `Fire()` method:

```csharp
// Fires a specific output port instead of the default 0
Fire(1);
Fire(port);
```
