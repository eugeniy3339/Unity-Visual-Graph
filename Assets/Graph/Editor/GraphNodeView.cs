using Graph.Logic;
using GraphEditor;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Graph.Editor
{
    internal sealed class GraphNodeView : Node
    {
        public GraphBlock Block { get; }
        public Port InputPort { get; private set; }
        public readonly Dictionary<GraphBlock.GraphOutputPort, Port> OutputPortsByPort = new();
        public GraphBlock.GraphOutputPort GetPort(Port port)
        {
            foreach (var portPortPair in OutputPortsByPort)
                if (portPortPair.Value == port)
                    return portPortPair.Key;

            return null;
        }
        public readonly Dictionary<GraphBlock.GraphValueInputPort, Port> ValueInputPortsByPort = new();
        public GraphBlock.GraphValueInputPort GetInput(Port port)
        {
            foreach (var inputPortPortPair in ValueInputPortsByPort)
                if (inputPortPortPair.Value == port)
                    return inputPortPortPair.Key;

            return null;
        }
        public readonly Dictionary<GraphBlock.GraphValueOutputPort, Port> ValueOutputPortsByPort = new();
        public GraphBlock.GraphValueOutputPort GetOutput(Port port)
        {
            foreach (var outputPortPortPair in ValueOutputPortsByPort)
                if (outputPortPortPair.Value == port)
                    return outputPortPortPair.Key;

            return null;
        }

        public GraphNodeView(GraphBlock block, SerializedProperty blockProperty)
        {
            Block = block;
            title = ObjectNames.NicifyVariableName(block.DisplayName);
            viewDataKey = block.Id;

            var pos = block.Position;
            SetPosition(new Rect(pos.x, pos.y, 0, 0));

            if (block.HasInputPort)
            {
                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
                InputPort.portName = "In";
                InputPort.userData = PortKind.Exec;
                inputContainer.Add(InputPort);
            }

            foreach (var outputPort in block.Ports)
                AddOutputPort(outputPort);

            foreach (var valuePort in block.ValueInputs)
                AddValueInputPort(valuePort);

            foreach (var valuePort in block.ValueOutputs)
                AddValueOutputPort(valuePort);

            if (blockProperty != null)
                BuildBody(blockProperty);

            RefreshExpandedState();
            RefreshPorts();
        }

        /// <summary>Creates and registers a new exec output port. No-op if it already exists.</summary>
        public Port AddOutputPort(GraphBlock.GraphOutputPort outputPort)
        {
            if (OutputPortsByPort.TryGetValue(outputPort, out Port existing))
                return existing;

            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            port.portName = outputPort.Name;
            port.userData = PortKind.Exec;
            OutputPortsByPort[outputPort] = port;
            outputContainer.Add(port);
            return port;
        }

        /// <summary>Removes an exec output port. The caller is responsible for detaching its edges first.</summary>
        public void RemoveOutputPort(GraphBlock.GraphOutputPort outputPort)
        {
            if (!OutputPortsByPort.TryGetValue(outputPort, out Port port))
                return;

            outputContainer.Remove(port);
            OutputPortsByPort.Remove(outputPort);
        }

        /// <summary>Creates and registers a new typed value input pin. No-op if it already exists.</summary>
        public Port AddValueInputPort(GraphBlock.GraphValueInputPort inputPort)
        {
            if (ValueInputPortsByPort.TryGetValue(inputPort, out Port existing))
                return existing;

            var port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, inputPort.Type);
            port.portName = inputPort.Name;
            port.portColor = PortColors.ForType(inputPort.Type);
            port.userData = PortKind.Value;
            ValueInputPortsByPort[inputPort] = port;
            inputContainer.Add(port);
            return port;
        }

        /// <summary>Removes a value input pin. The caller is responsible for detaching its edge first.</summary>
        public void RemoveValueInputPort(GraphBlock.GraphValueInputPort inputPort)
        {
            if (!ValueInputPortsByPort.TryGetValue(inputPort, out Port port))
                return;

            inputContainer.Remove(port);
            ValueInputPortsByPort.Remove(inputPort);
        }

        /// <summary>Creates and registers a new typed value output pin. No-op if it already exists.</summary>
        public Port AddValueOutputPort(GraphBlock.GraphValueOutputPort outputPort)
        {
            if (ValueOutputPortsByPort.TryGetValue(outputPort, out Port existing))
                return existing;

            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, outputPort.Type);
            port.portName = outputPort.Name;
            port.portColor = PortColors.ForType(outputPort.Type);
            port.userData = PortKind.Value;
            ValueOutputPortsByPort[outputPort] = port;
            outputContainer.Add(port);
            return port;
        }

        /// <summary>Removes a value output pin. The caller is responsible for detaching its edges first.</summary>
        public void RemoveValueOutputPort(GraphBlock.GraphValueOutputPort outputPort)
        {
            if (!ValueOutputPortsByPort.TryGetValue(outputPort, out Port port))
                return;

            outputContainer.Remove(port);
            ValueOutputPortsByPort.Remove(outputPort);
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
            Block.Position = new Vector2(newPos.xMin, newPos.yMin);
        }
    }
}
