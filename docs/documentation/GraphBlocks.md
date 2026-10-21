Graph Asset:

Creation of new block:
To create new Block Script just right click Project window, select Create, Graphs and then New Block Graph Script, or siply create new class, that inherits from GraphBlock.
To create new Block in the graph right click the graph's background, select Create Node and then select the block you want.

Block Script:
Block class must have Serializable Attribute on it, for it to save.
To configure block's "Group", "Name" when creating it in the graph add BlockMenu Attribute on it.
F.E.
[Serializable]
[BlockMenu("Custom/TestBlock")]
public class TestBlock : GraphBlock {

}

public:
DisplayName
DisplayName stores block display name. DisplayName can be overrided.

HasInputPort
HasInputPort stores if block have input port. HasInputPort can be overrided.

Ports
Read only list of GraphOutputPort. Ports can be overrided. The variable Ports returns should have SerializeReference/SerializeField on it, so the Ports can be saved.
F.E
[SerializeReference, HideInInspector] protected GraphOutputPort[] _ports = { new GraphOutputPort("Out") }; 
public virtual IReadOnlyList<GraphOutputPort> Ports => _ports;

ValueOutputs
Read only list of GraphValueOutputPort. ValueOutputs can be overrided. The variable ValueOutputs returns should have SerializeReference/SerializeField on it, so the ValueOutputs can be saved.
F.E
[SerializeReference, HideInInspector] protected GraphValueOutputPort[] _valueOutputs = { };
public virtual IReadOnlyList<GraphValueOutputPort> ValueOutputs => _valueOutputs;

ValueInputs
Read only list of GraphValueInputPort. ValueInputs can be overrided. The variable ValueInputs returns should have SerializeReference/SerializeField on it, so the ValueInputs can be saved.
F.E
[SerializeReference, HideInInspector] protected GraphValueInputPort[] _valueInputs = { }; 
public virtual IReadOnlyList<GraphValueInputPort> ValueInputs => _valueInputs;

StartBlock
Start Block is being called by GraphAsset on the first graph's block when StartGraph function is being called, or from other blocks, when theirs Fire function with port, that is connected to the block is being called. Start Block can be overrided (default calls Fire(0)).

GetInputValue
GetInputValue takes port name of the port it self and returns value of the input port.
