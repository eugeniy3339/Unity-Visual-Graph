using System;
using System.Collections.Generic;
using UnityEngine;

namespace Graph.Logic
{
    [Serializable]
    public class GraphBlock
    {
        [SerializeField, HideInInspector] private string _id;
        public string Id
        {
            get
            {
                if (string.IsNullOrEmpty(_id))
                    _id = Guid.NewGuid().ToString("N");
                return _id;
            }
        }

        [SerializeField, HideInInspector] private Vector2 _position = default;
        public Vector2 Position { get { return _position; } set { _position = value; } }

        [SerializeField, HideInInspector] private string _displayName = "Graph Block";
        public virtual string DisplayName { get { return _displayName; } set { _displayName = value; } }

        [SerializeField, HideInInspector] private bool _hasInputPort = true;
        public virtual bool HasInputPort { get { return _hasInputPort; } set { _hasInputPort = value; } }

        [SerializeReference, HideInInspector] protected GraphOutputPort[] _ports = { new GraphOutputPort("Out") };
        public virtual IReadOnlyList<GraphOutputPort> Ports => _ports;

        [SerializeReference, HideInInspector] protected GraphValueOutputPort[] _valueOutputs = { };
        public virtual IReadOnlyList<GraphValueOutputPort> ValueOutputs => _valueOutputs;

        [SerializeReference, HideInInspector] protected GraphValueInputPort[] _valueInputs = { };
        public virtual IReadOnlyList<GraphValueInputPort> ValueInputs => _valueInputs;

        public virtual void StartBlock() => Fire();

        protected void Fire(int portIndexToFire = 0)
        {
            if (Ports.Count <= portIndexToFire) return;
            Fire(Ports[portIndexToFire]);
        }

        protected void Fire(GraphOutputPort portToFire)
        {
            if (portToFire == null) return;
            portToFire.Fire();
        }



        public T GetInputValue<T>(string inputName)
        {
            foreach (var inputPort in ValueInputs)
            {
                if (inputPort.Name == inputName)
                {
                    return GetInputValue<T>(inputPort);
                }
            }
            return default;
        }

        public T GetInputValue<T>(GraphValueInputPort graphPort)
        {
            if (graphPort == null) return default;
            return graphPort.GetValue<T>();
        }

        private GraphValueOutputPort FindValueOutput(string outputName)
        {
            foreach (var outputPort in ValueOutputs)
            {
                if (outputPort.Name == outputName)
                {
                    return outputPort;
                }
            }
            return null;
        }

        public T GetOutputValue<T>(string outputName)
        {
            return GetOutputValue<T>(FindValueOutput(outputName));
        }

        public T GetOutputValue<T>(GraphValueOutputPort graphOutput)
        {
            if (graphOutput == null) return default;
            return graphOutput.GetValue<T>();
        }

        public bool SetOutputValue(string outputName, object value)
        {
            return SetOutputValue(FindValueOutput(outputName), value);
        }

        public bool SetOutputValue(GraphValueOutputPort port, object value)
        {
            if (port == null) return false;
            return port.SetValue(value);
        }



        public static GraphBlock CreateBlock(Type blockType)
        {
            if (blockType == null || !typeof(GraphBlock).IsAssignableFrom(blockType)) return null;

            GraphBlock block = (GraphBlock)Activator.CreateInstance(blockType);
            return block;
        }

        public static GraphBlock CreateBlock(Type blockType, Vector2 position)
        {
            GraphBlock graphBlock = CreateBlock(blockType);
            if (graphBlock != null)
            {
                graphBlock._position = position;
            }

            return graphBlock;
        }

        public static T CreateBlock<T>(string displayName) where T : GraphBlock
        {
            GraphBlock graphBlock = CreateBlock(typeof(T));
            if (graphBlock != null)
            {
                graphBlock.DisplayName = displayName;
            }

            return (T)graphBlock;
        }

        public static T CreateBlock<T>(Vector2 position, string displayName) where T : GraphBlock
        {
            GraphBlock graphBlock = CreateBlock(typeof(T));
            if (graphBlock != null)
            {
                graphBlock._position = position;
                graphBlock.DisplayName = displayName;
            }

            return (T)graphBlock;
        }




        [Serializable]
        public abstract class GraphPort
        {
            public string Name;

            public GraphPort(string name)
            {
                Name = name;
            }
        }

        [Serializable]
        public class GraphOutputPort : GraphPort
        {
            // SerializeReference: store a LINK to the real block in GraphAsset._blocks (not a private copy)
            [SerializeReference, HideInInspector] private List<GraphBlock> _connectedBlocks = new List<GraphBlock>();
            public IReadOnlyList<GraphBlock> ConnectedBlocks => _connectedBlocks;

            public void ConnectBlock(GraphBlock graphBlock)
            {
                if (graphBlock == null || _connectedBlocks.Contains(graphBlock)) return;
                _connectedBlocks.Add(graphBlock);
            }

            public void DisconnectBlock(GraphBlock graphBlock)
            {
                if (!_connectedBlocks.Contains(graphBlock)) return;
                _connectedBlocks.Remove(graphBlock);
            }

            public void Fire()
            {
                foreach (var connectedBlock in ConnectedBlocks)
                {
                    if (connectedBlock != null)
                        connectedBlock.StartBlock();
                }
            }

            public GraphOutputPort(string name) : base(name) { }
            public GraphOutputPort() : base(string.Empty) { }
        }

        [Serializable]
        public class GraphValueInputPort : GraphPort
        {
            // System.Type can't be serialized by Unity -> keep its name
            [SerializeField, HideInInspector] private string _typeName;
            public Type Type
            {
                get { return string.IsNullOrEmpty(_typeName) ? null : Type.GetType(_typeName); }
                set { _typeName = value != null ? value.AssemblyQualifiedName : null; }
            }

            // SerializeReference: must point at the SAME port / block instances that live in the other block
            [SerializeReference, HideInInspector] private GraphValueOutputPort _connectedPort;
            public GraphValueOutputPort ConnectedOutput { get { return _connectedPort; } }
            [SerializeReference, HideInInspector] private GraphBlock _connectedBlock;
            public GraphBlock ConnectedBlock { get { return _connectedBlock; } }

            public bool ConnectOutput(GraphBlock block, GraphValueOutputPort outputToConnect)
            {
                if (outputToConnect == null) return false;
                if (Type == null || outputToConnect.Type == null || !Type.IsAssignableFrom(outputToConnect.Type)) return false;
                if (_connectedPort != null) return false;
                _connectedPort = outputToConnect;
                _connectedBlock = block;

                return true;
            }

            public void DisconectCurOutput()
            {
                _connectedPort = null;
                _connectedBlock = null;
            }

            public T GetValue<T>()
            {
                if (_connectedPort == null) return default;
                return _connectedPort.GetValue<T>();
            }

            public GraphValueInputPort(string name, Type type) : base(name)
            {
                this.Type = type;
            }
            public GraphValueInputPort() : base(string.Empty) { }
        }

        [Serializable]
        public class GraphValueOutputPort : GraphPort
        {
            [SerializeField, HideInInspector] private string _typeName;
            public Type Type
            {
                get { return string.IsNullOrEmpty(_typeName) ? null : Type.GetType(_typeName); }
                set { _typeName = value != null ? value.AssemblyQualifiedName : null; }
            }

            // Runtime-only value (not serialized)
            public object Value { get; private set; }

            public T GetValue<T>()
            {
                if (!(Value is T)) return default;
                return (T)Value;
            }

            public bool SetValue(object value)
            {
                if (value == null || Type == null || !Type.IsAssignableFrom(value.GetType())) return false;
                this.Value = value;
                return true;
            }

            public GraphValueOutputPort(string name, Type type) : base(name)
            {
                this.Name = name;
                this.Type = type;
            }
            public GraphValueOutputPort() : base(string.Empty) { }
        }
    }
}
