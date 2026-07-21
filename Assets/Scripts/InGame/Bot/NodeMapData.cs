using System.Collections.Generic;
using UnityEngine;

namespace InGame.Bot
{
    [CreateAssetMenu(fileName = "NodeMapData", menuName = "ScriptableObjects/Bot/NodeMapData")]
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

        /// <summary>
        /// NodeDataSerializableをNodeDataに変換する
        /// </summary>
        /// <returns></returns>
        public List<NodeData> GetChangeNodeData()
        {
            List<NodeData> result = new();

            Dictionary<int, NodeData> indexDic = new();
            Dictionary<NodeData, NodeDataSerializable> serializableDic = new();

            //indexとNodeData , NodeDataと保存用NodeDataのペアを作る
            foreach (var data in _serializableNodeDatas)
            {
                NodeData nodeData = new(data.Position, data.Index);
                indexDic.Add(data.Index, nodeData);
                serializableDic.Add(nodeData, data);
            }

            //indexDicをもとにConnectとVaultの接続を行う
            foreach (var nodeDataPair in serializableDic)
            {
                foreach (var connectIndex in nodeDataPair.Value.ConnectNode)
                {
                    if (!indexDic.TryGetValue(connectIndex, out NodeData nodeData))
                    {
                        Debug.LogError("NodeDataが見つかりません");
                    }

                    nodeDataPair.Key.AddConnect(nodeData);
                }
                result.Add(nodeDataPair.Key);

                if (nodeDataPair.Value.VaultNode != -1)
                {
                    if (!indexDic.TryGetValue(nodeDataPair.Value.VaultNode, out NodeData nodeData))
                    {
                        Debug.LogError("NodeDataが見つかりません");
                    }

                    nodeDataPair.Key.SetVaultConnect(nodeData);
                }
            }

            return result;
        }

        public void ClearList()
        {
            _serializableNodeDatas.Clear();
        }

        /// <summary>
        /// ノードデータを保存用クラスに変換して保存する
        /// </summary>
        /// <param name="node">ノードリスト</param>
        public void AddNodeData(NodeData node)
        {
            List<int> connectIndex = new();

            Debug.Log("ConnectNodeCount" + node.ConnectNode.Count);
            foreach (NodeData connect in node.ConnectNode)
            {
                connectIndex.Add(connect.NodeIndex);
            }

            Debug.Log("indexCount" + connectIndex.Count);
            int vaultNode = node.VaultConnect?.NodeIndex ?? -1;
            _serializableNodeDatas.Add(new NodeDataSerializable(node.Position, node.NodeIndex, connectIndex.ToArray(), vaultNode));
        }
    }

    [System.Serializable]
    public class NodeDataSerializable
    {
        public Vector3 Position;
        public int Index;
        public int[] ConnectNode;
        public int VaultNode;
        public NodeDataSerializable(Vector3 pos, int index, int[] connect, int vaultNode = -1)
        {
            Position = pos;
            Index = index;
            ConnectNode = connect;
            VaultNode = vaultNode;
        }
    }
}
