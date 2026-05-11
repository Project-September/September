using System.Collections.Generic;
using UnityEngine;

namespace InGame.Bot
{
    /// <summary>
    /// NodeData‚ð‚Ç‚±‚Å‚àŽg‚¦‚é‚æ‚¤‚É‚·‚éƒNƒ‰ƒX
    /// </summary>
    public class NodeProvider : MonoBehaviour
    {
        [SerializeField] private NodeMapData _nodeMapData;
        [SerializeField] private bool _isDrawGizmo;
        public static NodeProvider Instance;
        private List<NodeData> _nodes;
        public List<NodeData> Nodes
        {
            get
            {
                if (_nodes == null || _nodes.Count == 0)
                {
                    _nodes = _nodeMapData.GetChangeNodeData();
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
            if (Application.isPlaying && _isDrawGizmo)
            {
                NodeGanerator.DrawGizmos(Nodes);
            }
        }

        public NodeData GetRandomNode()
        {
            return Nodes[Random.Range(0, Nodes.Count)];
        }
    }
}
