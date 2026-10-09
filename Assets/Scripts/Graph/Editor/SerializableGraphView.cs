using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphEditor
{
    /// <summary>
    /// GraphView bound to a single <see cref="GraphAsset"/>. Reads the asset to build
    /// nodes and edges, writes structure changes (nodes, connections, positions) back
    /// into the asset, and lets Unity's serialized-property drawers handle every field
    /// on each block.
    /// </summary>
    internal sealed class SerializableGraphView : GraphView
    {
        private readonly EditorWindow _window;
        private readonly BlockSearchWindowProvider _search;

        private GraphAsset _asset;
        private SerializedObject _serializedAsset;
        private bool _suppressChange;

        // Rebuilt every Rebuild() call; also used to sync ports without a full rebuild.
        private readonly Dictionary<GraphBlock, GraphNodeView> _views = new();

        // Blocks like a dialogue node derive their output ports from mutable data
        // (e.g. option text). Syncing on every keystroke would tear down edges the
        // user is actively connecting, so port sync is debounced; a redundant sync
        // call is a cheap no-op (SyncNodePorts diffs before touching anything).
        private const long PortSyncDebounceMs = 350;

        public SerializableGraphView(EditorWindow window)
        {
            _window = window;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            Insert(0, new GridBackground());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            _search = ScriptableObject.CreateInstance<BlockSearchWindowProvider>();
            _search.Init(CreateBlock);
            nodeCreationRequest = ctx =>
            {
                if (_asset != null)
                    SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), _search);
            };

            graphViewChanged = OnGraphViewChanged;
        }

        public GraphAsset Asset => _asset;

        // --- loading -------------------------------------------------------------

        public void Load(GraphAsset asset)
        {
            if (_asset != null && _asset != asset)
                Save();

            this.Unbind();
            _asset = asset;
            _serializedAsset = asset != null ? new SerializedObject(asset) : null;

            if (_serializedAsset != null)
            {
                // Persist inline field edits (message text, condition values, ...)
                // and, after a short idle delay, refresh any ports that depend on them
                // (e.g. a dialogue block's ports mirror its option texts).
                this.TrackSerializedObjectValue(_serializedAsset, so =>
                {
                    if (so.targetObject == null)
                        return;
                    EditorUtility.SetDirty(so.targetObject);
                    this.schedule.Execute(SyncAllPorts).ExecuteLater(PortSyncDebounceMs);
                });
            }

            Rebuild();
        }

        public void Rebuild()
        {
            _suppressChange = true;

            graphViewChanged = null;
            DeleteElements(graphElements.ToList());
            graphViewChanged = OnGraphViewChanged;
            _views.Clear();

            if (_asset == null)
            {
                _suppressChange = false;
                return;
            }

            EnsureEntryBlock();
            _serializedAsset?.Update();

            SerializedProperty blocksProp = _serializedAsset?.FindProperty("_blocks");
            if (blocksProp == null)
            {
                _suppressChange = false;
                return;
            }

            for (int i = 0; i < _asset.Blocks.Count; i++)
            {
                GraphBlock block = _asset.Blocks[i];
                if (block == null)
                    continue;

                SerializedProperty blockProp = blocksProp.GetArrayElementAtIndex(i);
                var view = new GraphNodeView(block, blockProp);
                AddElement(view);
                _views[block] = view;
            }

            foreach (GraphBlock block in _asset.Blocks)
            {
                if (block == null || !_views.TryGetValue(block, out GraphNodeView fromView))
                    continue;

                foreach (GraphConnection connection in block.Connections)
                {
                    if (connection?.target == null)
                        continue;
                    if (!_views.TryGetValue(connection.target, out GraphNodeView toView))
                        continue;
                    if (toView.InputPort == null)
                        continue;
                    if (!fromView.OutputPortsByName.TryGetValue(connection.fromPort, out Port outPort))
                        continue;

                    Edge edge = outPort.ConnectTo(toView.InputPort);
                    AddElement(edge);
                }
            }

            foreach (GraphBlock block in _asset.Blocks)
            {
                if (block == null || !_views.TryGetValue(block, out GraphNodeView toView))
                    continue;

                foreach (GraphValueConnection wire in block.ValueConnections)
                {
                    if (wire?.source == null)
                        continue;
                    if (!_views.TryGetValue(wire.source, out GraphNodeView fromView))
                        continue;
                    if (!fromView.ValueOutputPortsByName.TryGetValue(wire.fromPort, out Port outPort))
                        continue;
                    if (!toView.ValueInputPortsByName.TryGetValue(wire.inputPort, out Port inPort))
                        continue;

                    Edge edge = outPort.ConnectTo(inPort);
                    AddElement(edge);
                }
            }

            _suppressChange = false;
        }

        // --- dynamic ports -------------------------------------------------------

        /// <summary>Manual, immediate equivalent of the debounced auto-sync.</summary>
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
            foreach (GraphNodeView nodeView in _views.Values)
                anyChanged |= SyncNodePorts(nodeView);

            if (anyChanged)
                Save();
        }

        private bool SyncNodePorts(GraphNodeView nodeView)
        {
            bool changed = false;
            changed |= SyncExecOutputPorts(nodeView);
            changed |= SyncValuePorts(nodeView, nodeView.Block.ValueInputs, nodeView.ValueInputPortsByName,
                nodeView.AddValueInputPort, nodeView.RemoveValueInputPort);
            changed |= SyncValuePorts(nodeView, nodeView.Block.ValueOutputs, nodeView.ValueOutputPortsByName,
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
            IReadOnlyList<string> desired = nodeView.Block.OutputPorts;
            var desiredSet = new HashSet<string>(desired);

            List<string> toRemove = nodeView.OutputPortsByName.Keys
                .Where(name => !desiredSet.Contains(name)).ToList();
            List<string> toAdd = desired
                .Where(name => !nodeView.OutputPortsByName.ContainsKey(name)).ToList();

            if (toRemove.Count == 0 && toAdd.Count == 0)
                return false;

            foreach (string portName in toRemove)
            {
                Port port = nodeView.OutputPortsByName[portName];
                foreach (Edge edge in port.connections.ToList())
                {
                    ApplyEdgeRemoval(edge);
                    RemoveElement(edge);
                }
                nodeView.RemoveOutputPort(portName);
            }

            foreach (string portName in toAdd)
                nodeView.AddOutputPort(portName);

            return true;
        }

        /// <summary>Diffs one node's declared value ports (input or output side) against what's on screen.</summary>
        private bool SyncValuePorts(
            GraphNodeView nodeView,
            IReadOnlyList<GraphPort> desired,
            Dictionary<string, Port> existingByName,
            Func<GraphPort, Port> addPort,
            Action<string> removePort)
        {
            var desiredNames = new HashSet<string>(desired.Select(p => p.Name));

            List<string> toRemove = existingByName.Keys.Where(name => !desiredNames.Contains(name)).ToList();
            List<GraphPort> toAdd = desired.Where(p => !existingByName.ContainsKey(p.Name)).ToList();

            if (toRemove.Count == 0 && toAdd.Count == 0)
                return false;

            foreach (string portName in toRemove)
            {
                Port port = existingByName[portName];
                foreach (Edge edge in port.connections.ToList())
                {
                    ApplyEdgeRemoval(edge);
                    RemoveElement(edge);
                }
                removePort(portName);
            }

            foreach (GraphPort portDef in toAdd)
                addPort(portDef);

            return true;
        }

        // --- change handling ---------------------------------------------------

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_suppressChange || _asset == null)
                return change;

            bool structural = false;

            if (change.elementsToRemove != null)
            {
                change.elementsToRemove.RemoveAll(e => e is GraphNodeView { Block: EntryBlock });

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
                        nodeView.Block.EditorPosition = nodeView.GetPosition().position;
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

        // --- model mutation --------------------------------------------------------

        private void CreateBlock(Type blockType, Vector2 screenPosition)
        {
            if (_asset == null)
                return;

            var block = (GraphBlock)Activator.CreateInstance(blockType);
            _ = block.Id;
            block.EditorPosition = ScreenToGraphPosition(screenPosition);

            Undo.RegisterCompleteObjectUndo(_asset, "Add Block");
            _asset.Blocks.Add(block);
            Save();
            Rebuild();
        }

        private void RemoveBlock(GraphBlock block)
        {
            _asset.Blocks.Remove(block);
            if (_asset.Entry == block)
                _asset.Entry = null;

            // Drop dangling connections pointing at the removed block.
            foreach (GraphBlock other in _asset.Blocks)
            {
                other?.Connections.RemoveAll(c => c == null || c.target == block);
                other?.ValueConnections.RemoveAll(c => c == null || c.source == block);
            }
        }

        private static void ApplyEdgeCreation(Edge edge)
        {
            if (edge.output?.node is not GraphNodeView from || edge.input?.node is not GraphNodeView to)
                return;

            if (edge.output.userData is PortKind.Value)
            {
                string inputPort = edge.input.portName;
                string fromPort = edge.output.portName;
                // Value inputs are single-capacity: replace whatever was wired before.
                to.Block.ValueConnections.RemoveAll(c => c != null && c.inputPort == inputPort);
                to.Block.ValueConnections.Add(new GraphValueConnection(inputPort, fromPort, from.Block));
                return;
            }

            string port = edge.output.portName;
            if (from.Block.Connections.Any(c => c != null && c.fromPort == port && c.target == to.Block))
                return;

            from.Block.Connections.Add(new GraphConnection(port, to.Block));
        }

        private static void ApplyEdgeRemoval(Edge edge)
        {
            if (edge.output?.node is not GraphNodeView from || edge.input?.node is not GraphNodeView to)
                return;

            if (edge.output.userData is PortKind.Value)
            {
                string inputPort = edge.input.portName;
                to.Block.ValueConnections.RemoveAll(c => c != null && c.inputPort == inputPort && c.source == from.Block);
                return;
            }

            string port = edge.output.portName;
            from.Block.Connections.RemoveAll(c => c != null && c.fromPort == port && c.target == to.Block);
        }

        private void EnsureEntryBlock()
        {
            int before = _asset.Blocks.Count;
            _asset.EnsureDefaultBlocks();

            if (_asset.Blocks.Count == before)
                return;

            foreach (GraphBlock block in _asset.Blocks)
                _ = block.Id; // force-generate ids for freshly seeded blocks

            EditorUtility.SetDirty(_asset);
        }

        public void Save()
        {
            if (_asset == null)
                return;

            _asset.Entry ??= _asset.Blocks.FirstOrDefault(b => b is EntryBlock);

            if (_serializedAsset != null)
                _serializedAsset.Update();

            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssetIfDirty(_asset);
        }

        // --- graph plumbing ------------------------------------------------------

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
    }
}
