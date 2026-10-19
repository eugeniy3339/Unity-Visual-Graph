using Graph.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Graph.Editor
{
    internal sealed class SerializableGraphView : GraphView
    {
        private readonly EditorWindow _window;
        private readonly BlockSearchWindowProvider _search;

        private GraphAsset _asset;
        public GraphAsset Asset => _asset;

        private SerializedObject _serializedAsset;
        private bool _suppressChange;

        private readonly Dictionary<GraphBlock, GraphNodeView> _views = new();

        private const long PortSyncDebounceMs = 350;




        public SerializableGraphView(EditorWindow window) //Creating new graphView
        {
            _window = window;


            //Setting up window
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            Insert(0, new GridBackground());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            //Creating search component
            _search = ScriptableObject.CreateInstance<BlockSearchWindowProvider>();
            _search.Init(CreateBlockOnScreenPos);
            nodeCreationRequest = ctx =>
            {
                if (_asset != null)
                    SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), _search);
            };

            //Subscribing on event
            graphViewChanged = OnGraphViewChanged;
        }

        public void Load(GraphAsset asset)
        {
            //Saving cur asset
            if (_asset != null && _asset != asset)
                Save();

            //Sets asset to new one
            this.Unbind();
            _asset = asset;
            _serializedAsset = asset != null ? new SerializedObject(asset) : null;

            if (_serializedAsset != null)
            {
                // Persist inline field edits (message text, condition values, ...)
                // and, after a short idle delay, refresh any ports that depend on them
                // (e.g. a dialogue block's ports mirror its option texts).
                // Waits a bit after new serialized object created and then syncs all ports
                this.TrackSerializedObjectValue(_serializedAsset, so =>
                {
                    if (so.targetObject == null)
                        return;
                    EditorUtility.SetDirty(so.targetObject);
                    this.schedule.Execute(SyncAllPorts).ExecuteLater(PortSyncDebounceMs);
                });
            }

            //Rebuilds Graph
            Rebuild();
        }

        public void Rebuild()
        {
            _suppressChange = true;

            //Unsubscribes from event
            graphViewChanged = null;
            //Deletes all the graph elements
            DeleteElements(graphElements.ToList());
            //Subscribes from event
            graphViewChanged = OnGraphViewChanged;
            //Clears dictionary of blocks
            _views.Clear();

            if (_asset == null)
            {
                _suppressChange = false;
                return;
            }

            //Updates cur serialized asset
            _serializedAsset?.Update();

            //Gets list of blocks properties
            SerializedProperty blocksProp = _serializedAsset?.FindProperty("_blocks");
            if (blocksProp == null)
            {
                _suppressChange = false;
                return;
            }

            //For loop of blocks
            for (int i = 0; i < _asset.Blocks.Count; i++)
            {
                GraphBlock block = _asset.Blocks[i];
                if (block == null)
                    continue;

                //Gets serialized property of block
                SerializedProperty blockProp = blocksProp.GetArrayElementAtIndex(i);
                //Creates new GraphNodeView of block and its serializedProperty
                var view = new GraphNodeView(block, blockProp);
                AddElement(view);
                //Sets view property of block to _views dictionary
                _views[block] = view;
            }

            foreach (GraphBlock block in _asset.Blocks)
            {
                if (block == null || !_views.TryGetValue(block, out GraphNodeView fromView))
                    continue;

                foreach (var port in block.Ports)
                {
                    foreach (var connectedBlock in port.ConnectedBlocks)
                    {
                        if (connectedBlock == null)
                            continue;
                        if (!_views.TryGetValue(connectedBlock, out GraphNodeView toView))
                            continue;
                        if (toView.InputPort == null)
                            continue;
                        if (!fromView.OutputPortsByPort.TryGetValue(port, out Port outPort))
                            continue;

                        Edge edge = outPort.ConnectTo(toView.InputPort);
                        AddElement(edge);
                    }
                }
            }

            foreach (GraphBlock block in _asset.Blocks)
            {
                if (block == null || !_views.TryGetValue(block, out GraphNodeView toView))
                    continue;

                foreach (var input in block.ValueInputs)
                {
                    if (input.ConnectedOutput == null)
                        continue;

                    if (!_views.TryGetValue(input.ConnectedBlock, out GraphNodeView fromView))
                        continue;
                    if (!fromView.ValueOutputPortsByPort.TryGetValue(input.ConnectedOutput, out Port outPort))
                        continue;
                    if (!toView.ValueInputPortsByPort.TryGetValue(input, out Port inPort))
                        continue;

                    //Creates new edge, that connects ports
                    Edge edge = outPort.ConnectTo(inPort);
                    AddElement(edge);
                }
            }

            _suppressChange = false;
        }




        public void RefreshPortsNow() => SyncAllPorts();

        /// <summary>
        /// Re-reads <see cref="GraphBlock.OutputPorts"/> for every node and adds/removes
        /// ports to match, without rebuilding the whole graph (keeps zoom/pan/selection).
        /// Edges attached to a removed port are cleanly detached first.
        /// </summary>
        private void SyncAllPorts()
        {
            if (_asset == null)
                return;

            bool anyChanged = false;
            //Creates ports for the nodes
            foreach (GraphNodeView nodeView in _views.Values)
                anyChanged |= SyncNodePorts(nodeView);

            if (anyChanged)
                Save();
        }

        private bool SyncNodePorts(GraphNodeView nodeView)
        {
            bool changed = false;
            changed |= SyncExecOutputPorts(nodeView);
            changed |= SyncValuePorts(nodeView, nodeView.Block.ValueInputs, nodeView.ValueInputPortsByPort,
                nodeView.AddValueInputPort, nodeView.RemoveValueInputPort);
            changed |= SyncValuePorts(nodeView, nodeView.Block.ValueOutputs, nodeView.ValueOutputPortsByPort,
                nodeView.AddValueOutputPort, nodeView.RemoveValueOutputPort);

            if (changed)
            {
                nodeView.RefreshPorts();
                nodeView.RefreshExpandedState();
            }
            return changed;
        }

        private bool SyncExecOutputPorts(GraphNodeView nodeView)
        {
            IReadOnlyList<GraphBlock.GraphOutputPort> desired = nodeView.Block.Ports;
            var desiredSet = new HashSet<GraphBlock.GraphOutputPort>(desired);

            List<GraphBlock.GraphOutputPort> toRemove = nodeView.OutputPortsByPort.Keys
                .Where(name => !desiredSet.Contains(name)).ToList();

            List<GraphBlock.GraphOutputPort> toAdd = desired
                .Where(name => !nodeView.OutputPortsByPort.ContainsKey(name)).ToList();

            if (toRemove.Count == 0 && toAdd.Count == 0)
                return false;

            foreach (var portToRemove in toRemove)
            {
                Port port = nodeView.OutputPortsByPort[portToRemove];
                foreach (Edge edge in port.connections.ToList())
                {
                    ApplyEdgeRemoval(edge);
                    RemoveElement(edge);
                }

                nodeView.RemoveOutputPort(portToRemove);
            }

            foreach (var portToAdd in toAdd)
                nodeView.AddOutputPort(portToAdd);

            return true;
        }

        /// <summary>Diffs one node's declared value ports (input or output side) against what's on screen.</summary>
        private bool SyncValuePorts(
            GraphNodeView nodeView,
            IReadOnlyList<GraphBlock.GraphValueInputPort> desired,
            Dictionary<GraphBlock.GraphValueInputPort, Port> existingByPort,
            Func<GraphBlock.GraphValueInputPort, Port> addPort,
            Action<GraphBlock.GraphValueInputPort> removePort)
        {
            var desiredPorts = new HashSet<GraphBlock.GraphValueInputPort>(desired);

            List<GraphBlock.GraphValueInputPort> toRemove = existingByPort.Keys.Where(port => !desiredPorts.Contains(port)).ToList();
            List<GraphBlock.GraphValueInputPort> toAdd = desired.Where(p => !existingByPort.ContainsKey(p)).ToList();

            if (toRemove.Count == 0 && toAdd.Count == 0)
                return false;

            foreach (var portToRemove in toRemove)
            {
                Port port = existingByPort[portToRemove];
                foreach (Edge edge in port.connections.ToList())
                {
                    ApplyEdgeRemoval(edge);
                    RemoveElement(edge);
                }
                removePort(portToRemove);
            }

            foreach (var portToAdd in toAdd)
                addPort(portToAdd);

            return true;
        }

        private bool SyncValuePorts(
            GraphNodeView nodeView,
            IReadOnlyList<GraphBlock.GraphValueOutputPort> desired,
            Dictionary<GraphBlock.GraphValueOutputPort, Port> existingByPort,
            Func<GraphBlock.GraphValueOutputPort, Port> addPort,
            Action<GraphBlock.GraphValueOutputPort> removePort)
        {
            var desiredPorts = new HashSet<GraphBlock.GraphValueOutputPort>(desired);

            List<GraphBlock.GraphValueOutputPort> toRemove = existingByPort.Keys.Where(port => !desiredPorts.Contains(port)).ToList();
            List<GraphBlock.GraphValueOutputPort> toAdd = desired.Where(p => !existingByPort.ContainsKey(p)).ToList();

            if (toRemove.Count == 0 && toAdd.Count == 0)
                return false;

            foreach (var portToRemove in toRemove)
            {
                Port port = existingByPort[portToRemove];
                foreach (Edge edge in port.connections.ToList())
                {
                    ApplyEdgeRemoval(edge);
                    RemoveElement(edge);
                }
                removePort(portToRemove);
            }

            foreach (var portToAdd in toAdd)
                addPort(portToAdd);

            return true;
        }







        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_suppressChange || _asset == null)
                return change;

            bool structural = false;

            if (change.elementsToRemove != null)
            {
                Undo.RegisterCompleteObjectUndo(_asset, "Graph Edit");

                foreach (GraphElement element in change.elementsToRemove)
                {
                    switch (element)
                    {
                        case GraphNodeView nodeView:
                            RemoveBlock(nodeView.Block);
                            structural = true;
                            break;
                        case Edge edge:
                            ApplyEdgeRemoval(edge);
                            break;
                    }
                }
            }

            if (change.edgesToCreate is { Count: > 0 })
            {
                Undo.RegisterCompleteObjectUndo(_asset, "Graph Connect");
                foreach (Edge edge in change.edgesToCreate)
                    ApplyEdgeCreation(edge);
            }

            if (change.movedElements != null)
            {
                foreach (GraphElement element in change.movedElements)
                    if (element is GraphNodeView nodeView)
                        nodeView.Block.Position = nodeView.GetPosition().position;
            }

            Save();

            if (structural)
                EditorApplication.delayCall += DeferredRebuild;

            return change;
        }

        private void DeferredRebuild()
        {
            EditorApplication.delayCall -= DeferredRebuild;
            if (_window != null && _asset != null)
                Rebuild();
        }



        private void CreateBlockOnScreenPos(Type blockType, Vector2 screenPosition)
        {
            if (_asset == null)
                return;

            var block = (GraphBlock)Activator.CreateInstance(blockType);
            _ = block.Id;
            block.Position = ScreenToGraphPosition(screenPosition);

            Undo.RegisterCompleteObjectUndo(_asset, "Add Block");
            _asset.Blocks.Add(block);
            Save();
            Rebuild();
        }

        private void RemoveBlock(GraphBlock block)
        {
            _asset.Blocks.Remove(block);

            foreach (GraphBlock other in _asset.Blocks)
            {
                foreach (var port in other.Ports)
                {
                    if (port.ConnectedBlocks.Contains(block))
                        port.DisconnectBlock(block);
                }
                foreach (var inputPort in other.ValueInputs)
                {
                    if (inputPort.ConnectedBlock == block)
                        inputPort.DisconectCurOutput();
                }
            }
        }

        private static void ApplyEdgeCreation(Edge edge)
        {
            if (edge.output?.node is not GraphNodeView from || edge.input?.node is not GraphNodeView to)
                return;

            //Removes old port connection and adds new one
            if (edge.output.userData is PortKind.Value)
            {
                GraphBlock.GraphValueInputPort inputPort = to.GetInput(edge.input);
                if (inputPort == null) return;
                GraphBlock.GraphValueOutputPort fromPort = from.GetOutput(edge.output);

                inputPort.DisconectCurOutput();
                if (fromPort != null)
                    inputPort.ConnectOutput(to.Block, fromPort);

                return;
            }

            GraphBlock.GraphOutputPort port = from.GetPort(edge.output);
            if (port == null) return;
            if (port.ConnectedBlocks.Contains(to.Block)) return;

            port.ConnectBlock(to.Block); 
        }

        //Disconects edge and removes it from connected blocks
        private static void ApplyEdgeRemoval(Edge edge)
        {
            if (edge.output?.node is not GraphNodeView from || edge.input?.node is not GraphNodeView to)
                return;

            if (edge.output.userData is PortKind.Value)
            {
                GraphBlock.GraphValueInputPort inputPort = to.GetInput(edge.input);
                if (inputPort != null)
                    inputPort.DisconectCurOutput();
                return;
            }

            GraphBlock.GraphOutputPort port = from.GetPort(edge.output);
            if (port != null)
                port.DisconnectBlock(to.Block);
        }



        public void Save()
        {
            if (_asset == null)
                return;

            if (_serializedAsset != null)
                _serializedAsset.Update();

            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssetIfDirty(_asset);
        }





        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(port =>
                port.direction != startPort.direction &&
                port.node != startPort.node &&
                SameKind(port, startPort) &&
                (IsExec(port) || TypesCompatible(port, startPort))).ToList();
        }

        private static bool IsExec(Port port) => port.userData is PortKind.Exec;

        private static bool SameKind(Port a, Port b) =>
            a.userData is PortKind kindA && b.userData is PortKind kindB && kindA == kindB;

        private static bool TypesCompatible(Port a, Port b)
        {
            Port output = a.direction == Direction.Output ? a : b;
            Port input = a.direction == Direction.Output ? b : a;
            return input.portType.IsAssignableFrom(output.portType);
        }


        private Vector2 ScreenToGraphPosition(Vector2 screenPosition)
        {
            Vector2 windowLocal = screenPosition - _window.position.position;

            VisualElement root = _window.rootVisualElement;
            if (root?.parent != null)
                windowLocal = root.ChangeCoordinatesTo(root.parent, windowLocal);

            return contentViewContainer.WorldToLocal(windowLocal);
        }

        //Create node on screen pos
        //Add node
        //Remove node
        //Add edge
        //Remove edge
        //Save
        //Rebuild
    }
}
