using UnityEngine;

namespace Graph.Logic
{
    public class GraphStarter : MonoBehaviour
    {
        [SerializeField] private GraphAsset graph;

        private void Start()
        {
            graph?.StartGraph();
        }
    }
}
