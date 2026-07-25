using System;
using Cysharp.Threading.Tasks;
using Ingame.Tanihira;
using September.Common;
using September.InGame.Effect;
using UnityEngine;

namespace September.InGame.Player.Tanihira
{
    public class UltPenguinEffect : MonoBehaviour
    {
        [SerializeField] private FormationManager _formationManager;
        [SerializeField] private float _buffRate = 3f;
        [SerializeField] private EffectType _scaleEffect;
        [SerializeField] private float _effectDuration;
        [SerializeField] private float _effectEndScale = 3f;

        private Action<FriendBase> _endAction;

        public void PenguinsBecomeHuge()
        {
            foreach (FriendBase friend in _formationManager.CurrentFriendsList)
            {
                HugeSequence(friend).Forget();
            }
        }

        public void EndHuge()
        {
            foreach (FriendBase friend in _formationManager.CurrentFriendsList)
            {
                friend.Scale = 1f;
                friend.StopBuff();
            }
        }

        private async UniTaskVoid HugeSequence(FriendBase friend)
        {
            friend.IsVisible = false;
            friend.Scale = _effectEndScale;
            StaticServiceLocator.Instance.Get<EffectSpawner>().RequestPlayOneShotEffect(
                _scaleEffect,
                friend.transform.position,
                friend.transform.rotation
                );
            await UniTask.WaitForSeconds(_effectDuration);
            friend.IsVisible = true;
            friend.StartBuff(_buffRate);
        }
    }
}
