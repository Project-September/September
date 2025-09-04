using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ingame.Tanihira
{
    [CreateAssetMenu(fileName = "FriendStatus", menuName = "ScriptableObjects/FriendStatus")]
    public class FriendStatus : ScriptableObject
    {
        [SerializeField] private int _maxHealth;
        [SerializeField] private int _attackPower = 10;
        [SerializeField] private float _friendRotateSpeed = 120f;
        [SerializeField] private float _friendFormationMoveSpeed = 3.5f;
        [SerializeField] private float _frinedFormationDistance = 1.0f;
        [SerializeField] private float _friendChaseSpeed = 5f;
        [SerializeField] private float _frinedChaseStopDistance = 1.0f;
        [SerializeField] private float _friendStunTime = 10.0f;

        public int MaxHealth => _maxHealth;
        public int AttackPower => _attackPower;
        public float FriendRotateSpeed => _friendRotateSpeed;
        public float FriendFormationSpeed => _friendFormationMoveSpeed;
        public float FriendFormationDistance => _frinedFormationDistance;
        public float FriendChaseSpeed => _friendChaseSpeed;
        public float FriendChaseDistance => _frinedChaseStopDistance;
        public float FriendStunTime => _friendStunTime;
    }
}