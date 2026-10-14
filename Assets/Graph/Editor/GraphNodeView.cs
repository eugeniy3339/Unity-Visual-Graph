using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Graph.Logic;
using UnityEditor.UIElements;

namespace Graph.Editor
{
    /// <summary>
    /// Visual node representation of a GraphBlock in the editor canvas.
    /// Renders:
    ///   • A flow input port  (left  side, white)
    ///   • Flow output ports  (right side, white)
    ///   • Value input ports  (left  side, coloured by type)
    ///   • Value output ports (right side, coloured by type)
    ///   • Inline UIToolkit PropertyFields for every serializable field on the concrete block type
    /// </summary>
    public class GraphNodeView : Node
    {
        public GraphBlock Block { get; }

        // Port lists for index lookup when wiring edges to runtime data
        private readonly List<Port> _flowOutputPorts = new();
        private Port _flowInputPort;
        private readonly List<Port> _valueInputPorts  = new();
        private readonly List<Port> _valueOutputPorts = new();

        private static readonly Color FlowColor = new Color(0.9f, 0.9f, 0.9f);

        // ── Constructor ──────────────────────────────────────────────────────────
        /// <param name="block">The runtime block this node represents.</param>
        /// <param name="serializedObject">SerializedObject of the owning GraphAsset.</param>
        /// <param name="asset">The owning asset (needed to resolve the block's array index).</param>
        public GraphNodeView(GraphBlock block,
                             SerializedObject serializedObject = null,
                             GraphAsset asset = null)
        {
            Block = block;
            title = block.DisplayName;
            SetPosition(new Rect(new Vector2(block.position.x, block.position.y), Vector2.zero)); 

            BuildPorts();
            BuildSerializedFields(serializedObject, asset);

            RefreshExpandedState();
            RefreshPorts();
        }

        // ── Port construction ────────────────────────────────────────────────────
        private void BuildPorts()
        {
            // Flow input
            if (Block.hasAnInputPort)
            {
                _flowInputPort = InstantiatePort(Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi, typeof(bool));
                _flowInputPort.portName  = "In";
                _flowInputPort.portColor = FlowColor;
                inputContainer.Add(_flowInputPort);
            }

            // Value inputs
            foreach (var valIn in Block.ValueInputs)
            {
                var type  = valIn.type ?? typeof(object);
                var color = ColorFromSystemColor(valIn.outputColor);
                var port  = InstantiatePort(Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Single, type);
                port.portName  = valIn.name;
                port.portColor = color;
                port.userData  = type;
                _valueInputPorts.Add(port);
                inputContainer.Add(port);
            }

            // Flow outputs
            foreach (var outPort in Block.Ports)
            {
                var port = InstantiatePort(Orientation.Horizontal, Direction.Output,
                    Port.Capacity.Multi, typeof(bool));
                port.portName  = outPort.name;
                port.portColor = FlowColor;
                _flowOutputPorts.Add(port);
                outputContainer.Add(port);
            }

            // Value outputs
            foreach (var valOut in Block.ValueOutputs)
            {
                var type  = valOut.type ?? typeof(object);
                var color = ColorFromSystemColor(valOut.outputColor);
                var port  = InstantiatePort(Orientation.Horizontal, Direction.Output,
                    Port.Capacity.Multi, type);
                port.portName  = valOut.name;
                port.portColor = color;
                port.userData  = type;
                _valueOutputPorts.Add(port);
                outputContainer.Add(port);
            }
        }

        // ── Serialized field panel ───────────────────────────────────────────────
        private void BuildSerializedFields(SerializedObject serializedObject, GraphAsset asset)
        {
            var fieldContainer = new VisualElement();
            fieldContainer.style.paddingLeft   = 6;
            fieldContainer.style.paddingRight  = 6;
            fieldContainer.style.paddingTop    = 4;
            fieldContainer.style.paddingBottom = 4;
            fieldContainer.style.minWidth      = 180;

            bool hasFields = false;

            if (serializedObject != null && asset != null)
            {
                int index = asset.blocks.IndexOf(Block);

                if (index >= 0)
                {
                    // Use reflection to discover all user-serializable fields on the
                    // concrete block subclass (stops at GraphBlock — skips internal fields).
                    var fields = GetUserSerializableFields(Block.GetType());

                    foreach (var field in fields)
                    {
                        // Construct the full SerializedProperty path for this field
                        string propPath = $"blocks.Array.data[{index}].{field.Name}";
                        var prop = serializedObject.FindProperty(propPath);

                        // FindProperty returns null when [Serializable] is missing on the class
                        // or the field type is not Unity-serializable — skip gracefully.
                        if (prop == null) continue;

                        var propertyField = new PropertyField(prop,
                            ObjectNames.NicifyVariableName(field.Name));
                        // Bind wires the PropertyField to the live SerializedObject
                        propertyField.Bind(serializedObject);
                        fieldContainer.Add(propertyField);
                        hasFields = true;
                    }
                }
            }

            if (!hasFields)
            {
                var hint = new Label("No serializable fields");
                hint.style.unityTextAlign = TextAnchor.MiddleCenter;
                hint.style.color          = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
                hint.style.fontSize       = 10;
                hint.style.paddingTop     = 4;
                hint.style.paddingBottom  = 4;
                fieldContainer.Add(hint);
            }

            extensionContainer.Add(fieldContainer);
            expanded = true;
            RefreshExpandedState();
        }

        // ── Reflection helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Returns all fields that should be displayed in the node's field panel.
        /// Only includes fields declared in the concrete subclass hierarchy,
        /// stopping before GraphBlock (which owns internal structural fields).
        /// </summary>
        private static IReadOnlyList<FieldInfo> GetUserSerializableFields(Type type)
        {
            var result = new List<FieldInfo>();
            var seen   = new HashSet<string>();

            var current = type;
            while (current != null && current != typeof(GraphBlock))
            {
                var declared = current.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public   |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                foreach (var field in declared)
                {
                    if (seen.Contains(field.Name)) continue;
                    seen.Add(field.Name);

                    if (IsUserSerializableField(field))
                        result.Add(field);
                }

                current = current.BaseType;
            }

            // Reverse so base-class fields appear above derived-class fields
            result.Reverse();
            return result;
        }

        private static bool IsUserSerializableField(FieldInfo field)
        {
            // Explicitly excluded
            if (field.GetCustomAttribute<NonSerializedAttribute>() != null) return false;
            if (field.GetCustomAttribute<HideInInspector>()         != null) return false;

            // Included by Unity's serialization rules
            if (field.IsPublic)                                               return true;
            if (field.GetCustomAttribute<SerializeField>()     != null)      return true;
            if (field.GetCustomAttribute<SerializeReference>() != null)      return true;

            return false;
        }

        // ── Port accessors ───────────────────────────────────────────────────────
        public Port GetFlowInputPort()  => _flowInputPort;

        public Port GetFlowOutputPort(int index) =>
            index >= 0 && index < _flowOutputPorts.Count ? _flowOutputPorts[index] : null;

        public Port GetValueInputPort(int index) =>
            index >= 0 && index < _valueInputPorts.Count ? _valueInputPorts[index] : null;

        public Port GetValueOutputPort(int index) =>
            index >= 0 && index < _valueOutputPorts.Count ? _valueOutputPorts[index] : null;

        public int GetFlowOutputPortIndex(Port port)  => _flowOutputPorts.IndexOf(port);
        public int GetValueInputPortIndex(Port port)  => _valueInputPorts.IndexOf(port);
        public int GetValueOutputPortIndex(Port port) => _valueOutputPorts.IndexOf(port);

        // ── Position persistence ─────────────────────────────────────────────────
        public void CommitPosition()
        {
            var pos = GetPosition().position;
            Block.position = new Vector2(pos.x, pos.y);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        private static Color ColorFromSystemColor(Color c) =>
            new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
    }
}
