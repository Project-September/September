using System.Collections.Generic;
using UnityEngine;

namespace InGame.Bot
{
    [CreateAssetMenu(fileName = "NodeMapData", menuName = "ScriptableObject/NodeMapData")]
    public class NodeMapData : ScriptableObject
    {
        [SerializeField] private List<NodeDataSerializable> _serializableNodeDatas = new();

        public void SetNodeData(List<NodeData> data)
        {
            Debug.Log("SetData");
            _serializableNodeDatas.Clear();
            foreach (NodeData node in data)
            {
                AddNodeData(node);
            }
        }

        public List<NodeData> GetChengeNodeData()
        {
            List<NodeData> result = new();

            Dictionary<int, NodeData> indexDic = new();
            Dictionary<NodeData, NodeDataSerializable> serializableDic = new();

            foreach (var data in _serializableNodeDatas)
            {
                NodeData nodeData = new(data.Position, data.Index);
                indexDic.Add(data.Index, nodeData);
                serializableDic.Add(nodeData, data);
            }

            foreach (var nodeDataPair in serializableDic)
            {
                foreach (var connectIndex in nodeDataPair.Value.ConnectNode)
                {
                    if (!indexDic.TryGetValue(connectIndex, out NodeData nodeData))
                    {
                        Debug.LogError("NodeData‚ªŒ©‚Â‚©‚è‚Ü‚¹‚ñ");
                    }

                    nodeDataPair.Key.AddConnect(nodeData);
                }
                result.Add(nodeDataPair.Key);
            }

            return result;
        }

        public void ClearList()
        {
            _serializableNodeDatas.Clear();
        }

        public void AddNodeData(NodeData node)
        {
            List<int> connectIndex = new();

            Debug.Log("ConnectNodeCount" + node.ConnectNode.Count);
            foreach (NodeData connect in node.ConnectNode)
            {
                connectIndex.Add(connect.NodeIndex);
            }

            Debug.Log("indexCount" + connectIndex.Count);
            _serializableNodeDatas.Add(new NodeDataSerializable(node.Position, node.NodeIndex, connectIndex.ToArray()));
        }
    }

    [System.Serializable]
    public class NodeDataSerializable
    {
        public Vector3 Position;
        public int Index;
        public int[] ConnectNode;

        public NodeDataSerializable(Vector3 pos, int index, int[] connect)
        {
            Position = pos;
            Index = index;
            ConnectNode = connect;
        }
    }
}
