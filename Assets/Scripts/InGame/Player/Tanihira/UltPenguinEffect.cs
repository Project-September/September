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
                friend.transform.localScale = Vector3.one;
                friend.StopBuff();
            }
        }

        private async UniTaskVoid HugeSequence(FriendBase friend)
        {
            friend.gameObject.SetActive(false);
            friend.transform.localScale = Vector3.one * _effectEndScale;
            StaticServiceLocator.Instance.Get<EffectSpawner>().RequestPlayOneShotEffect(
                _scaleEffect,
                friend.transform.position,
                friend.transform.rotation
                );
            await UniTask.WaitForSeconds(_effectDuration);
            friend.gameObject.SetActive(true);
            friend.StartBuff(_buffRate);
        }
    }
}
