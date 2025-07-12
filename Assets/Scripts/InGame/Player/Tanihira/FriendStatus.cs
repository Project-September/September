using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ingame.Tanihira
{
    [CreateAssetMenu(fileName = "FriendzStatus", menuName = "ScriptableObjects/FriendDatabase")]
    public class FriendStatus : ScriptableObject
    {
        [Serializable]
        public struct FriendData
        {
            public FriendType _type;
            public GameObject _friendPrefab;
        }
    }
}