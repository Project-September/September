using System;
using System.Collections.Generic;
using UnityEngine;
namespace InGame.Bot
{
    [System.Serializable]
    public class NodeData
    {
        public Vector3 Position { get; private set; }
        public NodeState State { get; private set; }
        public int NodeIndex { get; private set; }
        public List<NodeData> ConnectNode { get; private set; }
        public NodeData Parent { get; private set; }
        public NodeData VaultConnect { get; private set; }
        public bool IsValut;
        public float Cost => GetCost();

        private float GetCost()
        {
            return StartDistance + _goalDistance - ConnectNode.Count;
        }

        public float StartDistance { get; private set; }
        private float _goalDistance;
        public NodeData(Vector3 positioin, int index)
        {
            Position = positioin;
            ConnectNode = new();
            NodeIndex = index;
            ResetState();
        }

        public NodeData(Vector3 position, int index, List<NodeData> connectNode)
        {
            Debug.Log(connectNode.Count);
            Position = position;
            ConnectNode = new List<NodeData>(connectNode);
            NodeIndex = index;
            ResetState();
        }

        public void AddConnect(NodeData data)
        {
            if (ConnectNode.Contains(data))
            {
                Debug.Log("èdï°");
                return;
            }

            ConnectNode.Add(data);
        }

        public void ResetState()
        {
            State = NodeState.None;
            StartDistance = 0;
            _goalDistance = 0;
            IsValut = false;
        }

        public float GetAllCost()
        {
            if (State == NodeState.None)
            {
                Debug.LogError("");
                return 0;
            }

            return StartDistance + _goalDistance;
        }

        public void OpenNode(float startDis, float goalDis)
        {
            StartDistance = startDis;
            _goalDistance = goalDis;

            State = NodeState.Open;
        }

        public void Clause()
        {
            State = NodeState.Closed;
        }

        public void SetParent(NodeData parent)
        {
            Parent = parent;
        }
        public void SetVaultConnect(NodeData nodeData)
        {
            VaultConnect = nodeData;
        }
    }

    public enum NodeState
    {
        None, Open, Closed
    }
}