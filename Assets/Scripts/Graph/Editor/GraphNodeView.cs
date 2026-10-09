using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphEditor
{
    /// <summary>
    /// Visual representation of one <see cref="GraphBlock"/>. Ports come from the
    /// block's <see cref="GraphBlock.OutputPorts"/>; the editable body is generated
    /// from the block's serialized fields, so any new field appears with no extra code.
    /// </summary>
    internal sealed class GraphNodeView : Node
    {
        public GraphBlock Block { get; }
        public Port InputPort { get; private set; }
        public readonly Dictionary<string, Port> OutputPortsByName = new();
        public readonly Dictionary<string, Port> ValueInputPortsByName = new();
        public readonly Dictionary<string, Port> ValueOutputPortsByName = new();

        public GraphNodeView(GraphBlock block, SerializedProperty blockProperty)
        {
            Block = block;
            title = ObjectNames.NicifyVariableName(block.DisplayName);
            viewDataKey = block.Id;

            var pos = block.EditorPosition;
            SetPosition(new Rect(pos.x, pos.y, 0, 0));

            if (block is EntryBlock)
                capabilities &= ~Capabilities.Deletable;

            if (block.HasInputPort)
            {
                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
                InputPort.portName = "In";
                InputPort.userData = PortKind.Exec;
                inputContainer.Add(InputPort);
            }

            foreach (var portName in block.OutputPorts)
                AddOutputPort(portName);

            foreach (GraphPort valuePort in block.ValueInputs)
                AddValueInputPort(valuePort);

            foreach (GraphPort valuePort in block.ValueOutputs)
                AddValueOutputPort(valuePort);

            if (blockProperty != null)
                BuildBody(blockProperty);

            RefreshExpandedState();
            RefreshPorts();
        }

        /// <summary>Creates and registers a new exec output port. No-op if it already exists.</summary>
        public Port AddOutputPort(string portName)
        {
            if (OutputPortsByName.TryGetValue(portName, out Port existing))
                return existing;

            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            port.portName = portName;
            port.userData = PortKind.Exec;
            OutputPortsByName[portName] = port;
            outputContainer.Add(port);
            return port;
        }

        /// <summary>Removes an exec output port. The caller is responsible for detaching its edges first.</summary>
        public void RemoveOutputPort(string portName)
        {
            if (!OutputPortsByName.TryGetValue(portName, out Port port))
                return;

            outputContainer.Remove(port);
            OutputPortsByName.Remove(portName);
        }

        /// <summary>Creates and registers a new typed value input pin. No-op if it already exists.</summary>
        public Port AddValueInputPort(GraphPort definition)
        {
            if (ValueInputPortsByName.TryGetValue(definition.Name, out Port existing))
                return existing;

            var port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, definition.Type);
            port.portName = definition.Name;
            port.portColor = PortColors.ForType(definition.Type);
            port.userData = PortKind.Value;
            ValueInputPortsByName[definition.Name] = port;
            inputContainer.Add(port);
            return port;
        }

        /// <summary>Removes a value input pin. The caller is responsible for detaching its edge first.</summary>
        public void RemoveValueInputPort(string portName)
        {
            if (!ValueInputPortsByName.TryGetValue(portName, out Port port))
                return;

            inputContainer.Remove(port);
            ValueInputPortsByName.Remove(portName);
        }

        /// <summary>Creates and registers a new typed value output pin. No-op if it already exists.</summary>
        public Port AddValueOutputPort(GraphPort definition)
        {
            if (ValueOutputPortsByName.TryGetValue(definition.Name, out Port existing))
                return existing;

            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, definition.Type);
            port.portName = definition.Name;
            port.portColor = PortColors.ForType(definition.Type);
            port.userData = PortKind.Value;
            ValueOutputPortsByName[definition.Name] = port;
            outputContainer.Add(port);
            return port;
        }

        /// <summary>Removes a value output pin. The caller is responsible for detaching its edges first.</summary>
        public void RemoveValueOutputPort(string portName)
        {
            if (!ValueOutputPortsByName.TryGetValue(portName, out Port port))
                return;

            outputContainer.Remove(port);
            ValueOutputPortsByName.Remove(portName);
        }

        private void BuildBody(SerializedProperty blockProperty)
        {
            var body = new VisualElement();
            body.style.paddingLeft = 6;
            body.style.paddingRight = 6;
            body.style.paddingTop = 4;
            body.style.paddingBottom = 4;
            body.style.minWidth = 180;

            SerializedProperty iterator = blockProperty.Copy();
            SerializedProperty end = iterator.GetEndProperty();
            bool enterChildren = true;
            int fieldCount = 0;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;

                switch (iterator.name)
                {
                    case "_id":
                    case "_editorPosition":
                    case "_connections":
                    case "_valueConnections":
                        continue;
                }

                var field = new PropertyField(iterator);
                field.BindProperty(iterator);
                body.Add(field);
                fieldCount++;
            }

            if (fieldCount > 0)
                extensionContainer.Add(body);
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Block.EditorPosition = new Vector2(newPos.xMin, newPos.yMin);
        }
    }
}
