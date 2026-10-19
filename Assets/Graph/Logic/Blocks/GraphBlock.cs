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

        [SerializeField, HideInInspector] protected GraphOutputPort[] _ports = { new GraphOutputPort("Out") }; 
        public virtual IReadOnlyList<GraphOutputPort> Ports => _ports;

        [SerializeField, HideInInspector] protected GraphValueOutputPort[] _valueOutputs = { };
        public virtual IReadOnlyList<GraphValueOutputPort> ValueOutputs => _valueOutputs;

        [SerializeField, HideInInspector] protected GraphValueInputPort[] _valueInputs = { }; 
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



        public object GetInputValue(string inputName)
        {
            foreach (var inputPort in ValueInputs)
            {
                if (inputPort.Name == inputName)
                {
                    return GetInputValue(inputPort);
                }
            }
            return null;
        }

        public object GetInputValue(GraphValueInputPort graphPort)
        {
            return graphPort.GetValue();
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

        public object GetOutputValue(string outputName)
        {
            return GetOutputValue(FindValueOutput(outputName));
        }

        public object GetOutputValue(GraphValueOutputPort graphOutput)
        {
            if (graphOutput == null) return null;
            return graphOutput.GetValue();
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
            if (!blockType.IsAssignableFrom(typeof(GraphBlock))) return null;

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

        public static GraphBlock CreateBlock(Type blockType, string displayName)
        {
            GraphBlock graphBlock = CreateBlock(blockType);
            if (graphBlock != null)
            {
                graphBlock.DisplayName = displayName;
            }

            return graphBlock;
        }

        public static GraphBlock CreateBlock(Type blockType, Vector2 position, string displayName)
        {
            GraphBlock graphBlock = CreateBlock(blockType);
            if(graphBlock != null)
            {
                graphBlock._position = position;
                graphBlock.DisplayName = displayName;
            }

            return graphBlock;
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
            [SerializeField, HideInInspector] private List<GraphBlock> _connectedBlocks = new List<GraphBlock>(); 
            public IReadOnlyList<GraphBlock> ConnectedBlocks => _connectedBlocks;

            public void ConnectBlock(GraphBlock graphBlock)
            {
                _connectedBlocks.Add(graphBlock);
            }

            public void DisconnectBlock(GraphBlock graphBlock)
            {
                if (!_connectedBlocks.Contains(graphBlock)) return;
                _connectedBlocks.Remove(graphBlock);
            }

            public void Fire()
            {
                foreach(var connectedBlock in ConnectedBlocks)
                {
                    connectedBlock.StartBlock();
                }
            }

            public GraphOutputPort(string name) : base(name) { }
        }

        [Serializable]
        public class GraphValueInputPort : GraphPort
        {
            public Type Type;
            [SerializeField, HideInInspector] private GraphValueOutputPort _connectedPort;
            public GraphValueOutputPort ConnectedOutput { get { return _connectedPort; } }
            [SerializeField, HideInInspector] private GraphBlock _connectedBlock;
            public GraphBlock ConnectedBlock { get { return _connectedBlock; } }

            public bool ConnectOutput(GraphBlock block, GraphValueOutputPort outputToConnect)
            {
                if (outputToConnect == null) return false;
                if (!Type.IsAssignableFrom(outputToConnect.Type)) return false;
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

            public object GetValue()
            {
                return _connectedPort.Value;
            }

            public GraphValueInputPort(string name, Type type) : base(name)
            {
                this.Type = type;
            }
        }

        [Serializable] 
        public class GraphValueOutputPort : GraphPort
        {
            public Type Type;
            public object Value { get; private set; }

            public object GetValue()
            {
                return Value;
            }

            public bool SetValue(object value)
            {
                if (!Type.IsAssignableFrom(value.GetType())) return false;
                this.Value = value;
                return true;
            }

            public GraphValueOutputPort(string name, Type type) : base(name)
            {
                this.Name = name;
                this.Type = type;
            }
        }
    }
}
