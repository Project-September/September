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
        [SerializeField] private float _friendFormationDistance = 1.0f;
        [SerializeField] private float _friendChaseSpeed = 5f;
        [SerializeField] private float _friendChaseStopDistance = 1.0f;
        [SerializeField] private float _friendStunTime = 10.0f;
        [SerializeField] private float _friendAccleration = 10.0f;

        public int MaxHealth { get => _maxHealth; set => _maxHealth = value; }
        public int AttackPower { get => _attackPower; set => _attackPower = value; }
        public float FriendRotateSpeed { get => _friendRotateSpeed; set => _friendRotateSpeed = value; }
        public float FriendFormationSpeed { get => _friendFormationMoveSpeed; set => _friendFormationMoveSpeed = value; }
        public float FriendFormationDistance { get => _friendFormationDistance; set => _friendFormationDistance = value; }
        public float FriendChaseSpeed { get => _friendChaseSpeed; set => _friendChaseSpeed = value; }
        public float FriendChaseDistance { get => _friendChaseStopDistance; set => _friendChaseStopDistance = value; }
        public float FriendStunTime { get => _friendStunTime; set => _friendStunTime = value; }
        public float FriendAccleration { get => _friendAccleration; set => _friendAccleration = value; }
        
        public FriendStatus Clone()
        {
            FriendStatus clone = ScriptableObject.CreateInstance<FriendStatus>();
            clone.MaxHealth = this.MaxHealth;
            clone.AttackPower = this.AttackPower;
            clone.FriendRotateSpeed = this.FriendRotateSpeed;
            clone.FriendFormationSpeed = this.FriendFormationSpeed;
            clone.FriendFormationDistance = this.FriendFormationDistance;
            clone.FriendChaseSpeed = this.FriendChaseSpeed;
            clone.FriendChaseDistance = this.FriendChaseDistance;
            clone.FriendStunTime = this.FriendStunTime;
            clone.FriendAccleration = this.FriendAccleration;
            return clone;
        }
    }
}