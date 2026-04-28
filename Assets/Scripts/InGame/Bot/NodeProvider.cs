using InGame.Bot;
using System.Collections.Generic;
using UnityEngine;

namespace InGame.Bot
{
    public class NodeProvider : MonoBehaviour
    {
        [SerializeField] private NodeMapData _nodeMapData;
        [SerializeField] private bool _isDrowGizmo;
        public static NodeProvider Instance;
        private List<NodeData> _nodes;
        public List<NodeData> Nodes
        {
            get
            {
                if (_nodes == null || _nodes.Count == 0)
                {
                    _nodes = _nodeMapData.GetChengeNodeData();
                }

                return _nodes;
            }
        }
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && _isDrowGizmo)
            {
                NodeGenerator.DrowGizmos(Nodes);
            }
        }
    }
}
