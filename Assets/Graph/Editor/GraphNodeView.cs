using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Graph.Logic;

namespace Graph.Editor
{
    /// <summary>
    /// Visual node representation of a GraphBlock in the editor canvas.
    /// Renders:
    ///   • A flow input port  (left  side, triangle shape)
    ///   • Flow output ports  (right side, one per GraphOutputPort)
    ///   • Value input ports  (left  side, coloured circles)
    ///   • Value output ports (right side, coloured circles)
    /// </summary>
    public class GraphNodeView : Node
    {
        public GraphBlock Block { get; }

        // Port lists for index lookup when wiring edges to runtime data
        private readonly List<Port> _flowOutputPorts = new();
        private Port _flowInputPort;
        private readonly List<Port> _valueInputPorts  = new();
        private readonly List<Port> _valueOutputPorts = new();

        // Port color palette for value types
        private static readonly Color FlowColor   = new Color(0.9f, 0.9f, 0.9f);
        private static readonly Color DefaultValueColor = new Color(0.5f, 0.8f, 1.0f);

        // ── Constructor ──────────────────────────────────────────────────────────
        public GraphNodeView(GraphBlock block)
        {
            Block = block;

            title = block.DisplayName;

            // Restore saved position
            SetPosition(new Rect(
                new Vector2(block.position.X, block.position.Y),
                Vector2.zero));

            BuildPorts();
            RefreshExpandedState();
            RefreshPorts();
        }

        // ── Port construction ────────────────────────────────────────────────────
        private void BuildPorts()
        {
            // ── Flow input ────────────────────────────────────────────────────────
            if (Block.hasAnInputPort)
            {
                _flowInputPort = InstantiatePort(Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi, typeof(bool));
                _flowInputPort.portName = "In";
                _flowInputPort.portColor = FlowColor;
                // No userData → identifies as a flow port
                inputContainer.Add(_flowInputPort);
            }

            // ── Value inputs ──────────────────────────────────────────────────────
            foreach (var valIn in Block.ValueInputs)
            {
                var type  = valIn.type ?? typeof(object);
                var color = ColorFromSystemColor(valIn.outputColor);
                var port  = InstantiatePort(Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Single, type);
                port.portName  = valIn.name;
                port.portColor = color;
                port.userData  = type; // marks as value port
                _valueInputPorts.Add(port);
                inputContainer.Add(port);
            }

            // ── Flow outputs ──────────────────────────────────────────────────────
            foreach (var outPort in Block.Ports)
            {
                var port = InstantiatePort(Orientation.Horizontal, Direction.Output,
                    Port.Capacity.Multi, typeof(bool));
                port.portName  = outPort.name;
                port.portColor = FlowColor;
                // No userData → flow port
                _flowOutputPorts.Add(port);
                outputContainer.Add(port);
            }

            // ── Value outputs ─────────────────────────────────────────────────────
            foreach (var valOut in Block.ValueOutputs)
            {
                var type  = valOut.type ?? typeof(object);
                var color = ColorFromSystemColor(valOut.outputColor);
                var port  = InstantiatePort(Orientation.Horizontal, Direction.Output,
                    Port.Capacity.Multi, type);
                port.portName  = valOut.name;
                port.portColor = color;
                port.userData  = type; // marks as value port
                _valueOutputPorts.Add(port);
                outputContainer.Add(port);
            }
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
            Block.position = new System.Numerics.Vector2(pos.x, pos.y);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        private static Color ColorFromSystemColor(System.Drawing.Color c) =>
            new Color(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
    }
}
