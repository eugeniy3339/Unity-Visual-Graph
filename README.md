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
* **Manual:** Inherit from `GraphBlock` and add the `[CreateAssetMenu]` attribute.

### Example Block Script
```csharp
[CreateAssetMenu("Custom/NewBlock")]
public class NewBlock : GraphBlock
{
}
```

## Handling Inputs & Outputs
Blocks can pass data between each other through ports.

### 1. Adding Inputs
Define an array of `GraphPort` objects and override `ValueInputs`:

```csharp
private GraphPort[] inputs = { new("Input", typeof(GameObject)) };
public override IReadOnlyList<GraphPort> ValueInputs => inputs;
```

### 2. Adding Outputs
Override `ValueOutputs` using the same structure:

```csharp
private GraphPort[] outputs = { new("Output", typeof(GameObject)) };
public override IReadOnlyList<GraphPort> ValueOutputs => outputs;
```

### 3. Getting & Setting Port Values

```csharp
// Get value from an input port
T value = GetInput<T>("portName");

// Set value on an output port
SetOutput("portName", value);

// Get value from an output port
T outputValue = GetOutputValue<T>("portName");
```