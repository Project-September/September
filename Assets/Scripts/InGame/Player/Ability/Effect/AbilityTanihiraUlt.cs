using System;
using System.Collections.Generic;
using System.Linq;
using Ingame.Tanihira;
using InGame.Tanihira;
using September.InGame.Player.Tanihira;
using UnityEngine;
using UnityEngine.Playables;

namespace InGame.Player.Ability.Effect
{
    [Serializable]
    public class AbilityTanihiraUlt : AbilityUltBase
    {
        [SerializeField] private float _duration = 10;
        [SerializeField] private int _spawnCount = 3;
        [SerializeField] private UltPenguinEffect _effect;
        [SerializeField] private Transform _penguinTransform;
        [SerializeField] private string _penguinAnimationTrackName;
        [SerializeField] private PlayableDirector _playableDirector;
        [SerializeField] private FormationManager _formationManager;
        [SerializeField] private FriendsSpawner _friendsSpawner;

        private List<FriendBase> _spawnedFriends = new();
        private FriendBase _firstPenguin;

        protected override void OnCutInStart()
        {
            if (_formationManager.CurrentFriendsList.Count == 0) return;

            // カットインで制御するペンギンを取得
            _firstPenguin = _formationManager.CurrentFriendsList[0];

            // 所定の位置に移動させる
            _firstPenguin.Agent.Warp(_penguinTransform.position);
            _firstPenguin.transform.rotation = _penguinTransform.rotation;

            // 一時的に移動を制限
            _firstPenguin.Agent.enabled = false;

            // Timelineに動的バインド
            PlayableBinding binding = _playableDirector.playableAsset.outputs.FirstOrDefault(c => c.streamName == _penguinAnimationTrackName);
            if (binding.streamName != _penguinAnimationTrackName)
            {
                Debug.Log($"{_penguinAnimationTrackName}トラックが見つかりませんでした");
                return;
            }
            _playableDirector.SetGenericBinding(binding.sourceObject, _firstPenguin.Animator);
        }

        protected override void OnCutInEnd()
        {
            var player = Parameter.Owner;

            for (int i = 0; i < _spawnCount; i++)
            {
                var friend = _friendsSpawner.SpawnFriend(FriendType.ChildPenguin, player.transform);
                _spawnedFriends.Add(friend);
            }

            if (_firstPenguin) _firstPenguin.Agent.enabled = true;
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
            _effect.EndHuge();
        }

        protected override void OnStartEffect()
        {
            _effect.PenguinsBecomeHuge();
        }
    }
}
