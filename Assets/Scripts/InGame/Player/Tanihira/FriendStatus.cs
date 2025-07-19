using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ingame.Tanihira
{
    [CreateAssetMenu(fileName = "FriendStatus", menuName = "ScriptableObjects/FriendStatus")]
    public class FriendStatus : ScriptableObject
    {
        [SerializeField] private float _friendRotateSpeed;
        [SerializeField] private float _friendMoveSpeed;
        [SerializeField] private float _friendFormationMoveSpeed = 3.5f;
        [SerializeField] private float _friendChaseSpeed;
        [SerializeField] private float _attackPower;

        public float FriendRotateSpeed => _friendRotateSpeed;
        public float FriendMoveSpeed => _friendMoveSpeed;
        public float FriendFormationSpeed => _friendFormationMoveSpeed;
        public float FriendChaseSpeed => _friendChaseSpeed;
        public float AttackPower => _attackPower;
    }
}