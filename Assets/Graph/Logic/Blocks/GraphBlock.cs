using System;
using System.Collections.Generic;
using UnityEngine;

namespace Graph.Logic
{
    [Serializable]
    public class GraphBlock
    {
        [SerializeReference] public Vector2 position = default;

        public virtual string DisplayName { get; set; } = "Graph Block";

        public virtual bool hasAnInputPort { get; set; } = true;

        [SerializeReference] protected List<GraphOutputPort> ports = new List<GraphOutputPort> { new GraphOutputPort { name = "Out" } };
        public virtual IReadOnlyList<GraphOutputPort> Ports => ports;

        [SerializeReference] protected List<GraphValueOutputPort> valueOutputs = new List<GraphValueOutputPort>();
        public virtual IReadOnlyList<GraphValueOutputPort> ValueOutputs => valueOutputs;

        [SerializeReference] protected List<GraphValueInputPort> valueInputs = new List<GraphValueInputPort>();
        public virtual IReadOnlyList<GraphValueInputPort> ValueInputs => valueInputs;
         


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
                if (inputPort.name == inputName)
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
                if (outputPort.name == outputName)
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



        [Serializable]
        public class GraphOutputPort
        {
            public string name;
            [SerializeReference] private List<GraphBlock> connectedBlocks = new List<GraphBlock>(); 
            public IReadOnlyList<GraphBlock> ConnectedBlocks => connectedBlocks;

            public void ConnectBlock(GraphBlock graphBlock)
            {
                connectedBlocks.Add(graphBlock);
            }

            public void DisconnectBlock(GraphBlock graphBlock)
            {
                if (!connectedBlocks.Contains(graphBlock)) return;
                connectedBlocks.Remove(graphBlock);
            }

            public void Fire()
            {
                foreach(var connectedBlock in ConnectedBlocks)
                {
                    connectedBlock.StartBlock();
                }
            }
        }

        [Serializable]
        public class GraphValueInputPort
        {
            public string name;
            public Type type;
            public Color outputColor = Color.blue;
            [SerializeReference] private GraphValueOutputPort _connectedPort;
            public GraphValueOutputPort connectedPort { get { return _connectedPort; } }

            public bool ConnectOutput(GraphValueOutputPort outputToConnect)
            {
                if (outputToConnect == null) return false;
                if (!type.IsAssignableFrom(outputToConnect.type)) return false;
                if (_connectedPort != null) return false;
                _connectedPort = outputToConnect;

                return true;
            }

            public void DisconectCurOutput()
            {
                _connectedPort = null;
            }

            public object GetValue()
            {
                return _connectedPort.value;
            }
        }

        [Serializable] 
        public class GraphValueOutputPort
        {
            public string name;
            public Type type;
            public Color outputColor = Color.blue;
            public object value { get; private set; }

            public object GetValue()
            {
                return value;
            }

            public bool SetValue(object value)
            {
                if (!type.IsAssignableFrom(value.GetType())) return false;
                this.value = value;
                return true;
            }
        }
    }
}
