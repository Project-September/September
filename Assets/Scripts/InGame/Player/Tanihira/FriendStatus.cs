using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ingame.Tanihira
{
    [CreateAssetMenu(fileName = "FriendStatus", menuName = "ScriptableObjects/FriendStatus")]
    public class FriendStatus : ScriptableObject
    {
        [SerializeField] private float _friendRotateSpeed = 120f;
        [SerializeField] private float _friendMoveSpeed = 3.5f;
        [SerializeField] private float _friendFormationMoveSpeed = 3.5f;
        [SerializeField] private float _friendChaseSpeed = 5f;
        [SerializeField] private float _attackPower = 10f;

        public float FriendRotateSpeed => _friendRotateSpeed;
        public float FriendMoveSpeed => _friendMoveSpeed;
        public float FriendFormationSpeed => _friendFormationMoveSpeed;
        public float FriendChaseSpeed => _friendChaseSpeed;
        public float AttackPower => _attackPower;
    }
}