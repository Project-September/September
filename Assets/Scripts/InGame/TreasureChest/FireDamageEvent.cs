using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Health;
using September.Common;
using UnityEngine;

namespace InGame.TreasureChest
{
    /// <summary>
    /// 周囲に炎を発生させ、一定間隔でダメージを与える宝箱イベント。
    /// </summary>
    public class FireDamageEvent : TreasureChestEventBase
    {
        [SerializeField] private GameObject _fireEffect;
        [SerializeField] private float _damageRange;
        [SerializeField] private int _damageAmount;
        [SerializeField] private float _damageInterval;
        [SerializeField] private float _duration;

        private CancellationTokenSource _cancellationTokenSource;
        private readonly List<GameObject> _targetPlayer = new();

        public override void Trigger(TriggerContext context)
        {
            Cancel();

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                context.TreasureChestObject.gameObject.GetCancellationTokenOnDestroy());

            FireDamageAsync(
                context,
                _cancellationTokenSource,
                _cancellationTokenSource.Token).Forget();
        }

        private async UniTask FireDamageAsync(TriggerContext context, CancellationTokenSource cancellationTokenSource, CancellationToken token)
        {
            _fireEffect.SetActive(true);

            try
            {
                float elapsedTime = 0f;

                while (elapsedTime < _duration)
                {
                    token.ThrowIfCancellationRequested();

                    foreach (GameObject target in GetDamageTarget(context.CenterPosition))
                    {
                        AddDamage(target);
                    }

                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_damageInterval),
                        cancellationToken: token);

                    elapsedTime += _damageInterval;
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                // 自分が現在実行中のイベントならエフェクトを消す
                if (_cancellationTokenSource == cancellationTokenSource)
                {
                    _fireEffect.SetActive(false);

                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
            }
        }

        private List<GameObject> GetDamageTarget(Vector3 centerPos)
        {
            _targetPlayer.Clear();

            foreach (var kv in PlayerDatabase.Instance.PlayerObjectDic)
            {
                float sqrDistance =
                    (kv.Value.transform.position - centerPos).sqrMagnitude;

                if (sqrDistance <= _damageRange * _damageRange)
                {
                    _targetPlayer.Add(kv.Value.gameObject);
                }
            }

            return _targetPlayer;
        }

        private void AddDamage(GameObject target)
        {
            if (!target.TryGetComponent(out IDamageable damageable))
            {
                return;
            }

            var hitData = new HitData(
                HitActionType.Damage,
                _damageAmount,
                PlayerRef.None,
                damageable.OwnerPlayerRef);

            damageable.TakeHit(ref hitData);
        }

        private void Cancel()
        {
            if (_cancellationTokenSource == null)
            {
                return;
            }

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }
}
