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
        public List<NodeData> Nodes => _nodeMapData.NodeDatas;
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
