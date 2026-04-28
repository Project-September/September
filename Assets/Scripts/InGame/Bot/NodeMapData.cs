using System.Collections.Generic;
using UnityEngine;

namespace InGame.Bot
{
    [CreateAssetMenu(fileName ="NodeMapData",menuName = "ScriptableObject/NodeMapData")]
    public class NodeMapData : ScriptableObject
    {
        [SerializeField] private List<NodeData> _nodeDatas  = new();
        public List<NodeData> NodeDatas => new(_nodeDatas);

        public void SetNodeData(List<NodeData> data)
        {
            Debug.Log("SetData");
            _nodeDatas.Clear();  
            foreach(NodeData node in data)
            {
                _nodeDatas.Add(new NodeData(node.Position,node.NodeIndex,node.ConnectNode));
            }
        }

        public void ClearList()
        {
            _nodeDatas.Clear();
        }

        public void AddNodeData(NodeData node)
        {
            _nodeDatas.Add(new NodeData(node.Position, node.NodeIndex, node.ConnectNode));
        }
    }
}
