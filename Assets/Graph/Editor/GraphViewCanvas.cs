using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Graph.Logic;

namespace Graph.Editor
{
    /// <summary>
    /// The UIToolkit GraphView canvas.
    /// Handles: node rendering, connection drawing, right-click context menu (block search).
    /// </summary>
    public class GraphViewCanvas : UnityEditor.Experimental.GraphView.GraphView
    {
        private readonly GraphAsset _asset;
        private readonly GraphEditorWindow _window;

        // Maps runtime block instances to their node views.
        private readonly Dictionary<GraphBlock, GraphNodeView> _nodeMap = new();

        // Shared SerializedObject — all node inline field panels read from this single instance.
        private readonly SerializedObject _serializedAsset;

        // ── Constructor ──────────────────────────────────────────────────────────
        public GraphViewCanvas(GraphAsset asset, GraphEditorWindow window)
        {
            _asset = asset;
            _window = window;
            _serializedAsset = new SerializedObject(asset);

            // Standard GraphView setup
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // Grid background
            var grid = new GridBackground();
            grid.StretchToParentSize();
            Insert(0, grid);

            // Right-click → block search
            this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));

            // Edge connection validation & creation
            graphViewChanged = OnGraphViewChanged;

            // Populate nodes from the asset
            LoadNodes();
            LoadEdges();
        }

        // ── Port compatibility ───────────────────────────────────────────────────
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatible = new List<Port>();
            ports.ForEach(port =>
            {
                if (port == startPort) return;
                if (port.node == startPort.node) return;
                if (port.direction == startPort.direction) return;

                // For value ports: check type compatibility stored in userData
                if (startPort.userData is Type startType && port.userData is Type portType)
                {
                    if (!AreTypesCompatible(startType, portType, startPort.direction)) return;
                }
                // Flow ports (no userData) are always compatible with flow ports
                else if (startPort.userData != null || port.userData != null)
                {
                    return; // Don't mix flow and value ports
                }

                compatible.Add(port);
            });
            return compatible;
        }

        private static bool AreTypesCompatible(Type a, Type b, Direction aDir)
        {
            Type outputType = aDir == Direction.Output ? a : b;
            Type inputType  = aDir == Direction.Output ? b : a;
            return inputType.IsAssignableFrom(outputType);
        }

        // ── Node loading ─────────────────────────────────────────────────────────
        private void LoadNodes()
        {
            var blocks = _asset.GetBlocks();
            if (blocks == null) return;

            foreach (var block in blocks)
            {
                if (block == null) continue;
                var nodeView = new GraphNodeView(block, _serializedAsset, _asset);
                _nodeMap[block] = nodeView;
                AddElement(nodeView);
            }
        }

        private void LoadEdges()
        {
            var blocks = _asset.GetBlocks();
            if (blocks == null) return;

            foreach (var block in blocks)
            {
                if (block == null) continue;
                if (!_nodeMap.TryGetValue(block, out var srcNodeView)) continue;

                // Flow port edges
                for (int pi = 0; pi < block.Ports.Count; pi++)
                {
                    var outPort = block.Ports[pi];
                    if (outPort?.ConnectedBlocks == null) continue;

                    var srcPort = srcNodeView.GetFlowOutputPort(pi);
                    if (srcPort == null) continue;

                    foreach (var connectedBlock in outPort.ConnectedBlocks)
                    {
                        if (connectedBlock == null) continue;
                        if (!_nodeMap.TryGetValue(connectedBlock, out var dstNodeView)) continue;

                        var dstPort = dstNodeView.GetFlowInputPort();
                        if (dstPort == null) continue;

                        var edge = srcPort.ConnectTo(dstPort);
                        AddElement(edge);
                    }
                }

                // Value output → value input edges
                foreach (var valueInput in block.ValueInputs)
                {
                    if (valueInput?.connectedPort == null) continue;

                    // Find the source block that owns the connected output port
                    foreach (var srcBlock in blocks)
                    {
                        if (srcBlock == null) continue;
                        int idx = srcBlock.ValueOutputs.ToList().IndexOf(valueInput.connectedPort);
                        if (idx < 0) continue;

                        if (!_nodeMap.TryGetValue(srcBlock, out var valSrcView)) break;
                        if (!_nodeMap.TryGetValue(block, out var valDstView)) break;

                        var srcValPort = valSrcView.GetValueOutputPort(idx);
                        var dstValPort = valDstView.GetValueInputPort(block.ValueInputs.ToList().IndexOf(valueInput));

                        if (srcValPort != null && dstValPort != null)
                        {
                            var edge = srcValPort.ConnectTo(dstValPort);
                            AddElement(edge);
                        }
                        break;
                    }
                }
            }
        }

        // ── GraphView change callback ────────────────────────────────────────────
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            // Handle new edges (connections)
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    ApplyNewEdge(edge);
                }
            }

            // Handle element removal
            if (change.elementsToRemove != null)
            {
                foreach (var element in change.elementsToRemove)
                {
                    switch (element)
                    {
                        case Edge removedEdge:
                            RemoveEdge(removedEdge);
                            break;
                        case GraphNodeView removedNode:
                            RemoveNode(removedNode);
                            break;
                    }
                }
            }

            // Persist position changes
            if (change.movedElements != null)
            {
                foreach (var moved in change.movedElements)
                {
                    if (moved is GraphNodeView nodeView)
                        nodeView.CommitPosition();
                }
            }

            EditorUtility.SetDirty(_asset);
            return change;
        }

        private void ApplyNewEdge(Edge edge)
        {
            var outputNode = edge.output.node as GraphNodeView;
            var inputNode  = edge.input.node  as GraphNodeView;
            if (outputNode == null || inputNode == null) return;

            bool isFlow = edge.output.userData == null;

            if (isFlow)
            {
                int portIndex = outputNode.GetFlowOutputPortIndex(edge.output);
                if (portIndex >= 0 && portIndex < outputNode.Block.Ports.Count)
                {
                    outputNode.Block.Ports[portIndex].ConnectBlock(inputNode.Block);
                }
            }
            else
            {
                int srcValIdx = outputNode.GetValueOutputPortIndex(edge.output);
                int dstValIdx = inputNode.GetValueInputPortIndex(edge.input);

                if (srcValIdx >= 0 && dstValIdx >= 0)
                {
                    var inputPort  = inputNode.Block.ValueInputs[dstValIdx];
                    var outputPort = outputNode.Block.ValueOutputs[srcValIdx];
                    inputPort.ConnectOutput(outputPort);
                }
            }
        }

        private void RemoveEdge(Edge edge)
        {
            var outputNode = edge.output?.node as GraphNodeView;
            var inputNode  = edge.input?.node  as GraphNodeView;
            if (outputNode == null || inputNode == null) return;

            bool isFlow = edge.output.userData == null;

            if (isFlow)
            {
                int portIndex = outputNode.GetFlowOutputPortIndex(edge.output);
                if (portIndex >= 0 && portIndex < outputNode.Block.Ports.Count)
                {
                    outputNode.Block.Ports[portIndex].DisconnectBlock(inputNode.Block);
                }
            }
            else
            {
                int dstValIdx = inputNode.GetValueInputPortIndex(edge.input);
                if (dstValIdx >= 0)
                {
                    inputNode.Block.ValueInputs[dstValIdx].DisconectCurOutput();
                }
            }
        }

        private void RemoveNode(GraphNodeView nodeView)
        {
            _asset.RemoveBlock(nodeView.Block);
            _nodeMap.Remove(nodeView.Block);
            // Keep SerializedObject in sync after structural change
            _serializedAsset.Update();
        }

        // ── Context menu (right-click to add blocks) ─────────────────────────────
        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            // Only show "Add Node" when right-clicking on the background
            if (evt.target is GraphViewCanvas || evt.target is GridBackground)
            {
                var mousePos = evt.localMousePosition;
                evt.menu.AppendAction("Add Node", _ => OpenBlockSearch(mousePos));
            }
        }

        private void OpenBlockSearch(Vector2 screenPosition)
        {
            var worldPos = contentViewContainer.WorldToLocal(screenPosition);
            BlockSearchWindow.Open(_asset, worldPos, block =>
            {
                if (block == null) return;
                _asset.AddBlock(block);

                // Sync the SerializedObject so the new array element is visible
                _serializedAsset.Update();

                var nodeView = new GraphNodeView(block, _serializedAsset, _asset);
                _nodeMap[block] = nodeView;
                AddElement(nodeView);
                EditorUtility.SetDirty(_asset);
            });
        }

        // ── Commit all positions ─────────────────────────────────────────────────
        public void CommitPositions()
        {
            foreach (var (block, view) in _nodeMap)
                view.CommitPosition();
        }
    }
}
