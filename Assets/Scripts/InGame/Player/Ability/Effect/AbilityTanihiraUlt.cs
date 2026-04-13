using System;
using System.Collections.Generic;
using Ingame.Tanihira;
using InGame.Tanihira;
using UnityEngine;

namespace InGame.Player.Ability.Effect
{
    [Serializable]
    public class AbilityTanihiraUlt : AbilityUltBase
    {
        [SerializeField] private float _duration = 10;
        [SerializeField] private int _spawnCount = 3;
        
        private FriendsSpawner _friendsSpawner;
        private FormationManager _formationManager;
        private List<FriendBase> _spawnedFriends = new();
        
        protected override void OnCutInEnd()
        {
            var player = Parameter.Owner;

            if (!_friendsSpawner) _friendsSpawner = player.GetComponent<FriendsSpawner>();
            if (!_formationManager) _formationManager = player.GetComponent<FormationManager>();

            for (int i = 0; i < _spawnCount; i++)
            {
                var friend = _friendsSpawner.SpawnFriend(FriendType.ChildPenguin, player.transform);
                _spawnedFriends.Add(friend);
            }
        }

        protected override void OnUpdateUlt(float deltaTime)
        {
            if (TimeSinceCutInEnd > _duration)
            {
                RequestEndAbility();
            }
        }

        protected override void OnEndUlt()
        {
            foreach (var friend in _spawnedFriends)
            {
                _formationManager.DeleteFriend(friend);
                Runner.Despawn(friend.Object);
            }
            _spawnedFriends.Clear();
        }
    }
}