using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Jewelry.Common;
using September.Common;
using September.InGame.Effect;
using September.InGame.Fields;
using September.InGame.Jewelry;
using UnityEngine;

namespace InGame.Jewelry
{
    public class Jewelry : NetworkBehaviour, IJewelry
    {
        [Header("この宝石のパラメータ群")]
        [SerializeField] JewelryInfo _jewelryParams;
        [SerializeField] JewelryControl _jewelryControl;

        [Header("宝石エフェクト")]
        [SerializeField] private ParticleSystem _jewelryEffect;
        [SerializeField] private ParticleSystemRenderer[] _renderers;

        [Header("出現エフェクト")]
        [SerializeField] private ParticleSystem _spawnEffect;
        [SerializeField] private float _spawnTimeOffset = 0.9f;

        public JewelryInfo JewelryParams => _jewelryParams;
        public JewelryControl JewelryControl => _jewelryControl;

        private TickTimer _despawnTimer;

        [Networked] private NetworkBool IsSpawnEffectPlaying { get; set; }
        [Networked] private TickTimer SpawnEffectTimer { get; set; }

        private bool _isSpawnEffectPlayingLocal;

        public override void Spawned()
        {
            _despawnTimer = TickTimer.CreateFromSeconds(Runner, _jewelryParams.LifeTime);
        }

        public override void FixedUpdateNetwork()
        {
            // スポーンエフェクトの再生完了時（宝石を出現させるタイミング）
            if (IsSpawnEffectPlaying && SpawnEffectTimer.Expired(Runner))
            {
                _jewelryControl.UnFreeze();
                IsSpawnEffectPlaying = false;
                SpawnEffectTimer = TickTimer.None;
            }

            // 自動消滅時間を過ぎたらデスポーン
            if (_despawnTimer.Expired(Runner))
            {
                Despawn();
            }

            // 範囲外に出たら即時デスポーン
            if (OutOfFieldArea.I.IsOutOfField(transform.position + Vector3.up * _jewelryParams.FallDepth))
            {
                PlayPickupEffect();
                Despawn();
            }

            return;

            // 自動消滅時の処理
            void Despawn()
            {
                Runner.Despawn(Object);
                DespawnedJewelryRepository.AddDespawnedJewelry(_jewelryParams.JewelryType);
            }
        }

        public override void Render()
        {
            // スポーン演出
            if (_isSpawnEffectPlayingLocal != IsSpawnEffectPlaying)
            {
                _isSpawnEffectPlayingLocal = IsSpawnEffectPlaying;
                OnChangedIsSpawnEffectPlaying();
            }

            // 消滅前の点滅演出
            if (_despawnTimer.RemainingTime(Runner) <= _jewelryParams.BlinkStartRemainingTime)
            {
                bool blink = Runner.SimulationTime * _jewelryParams.BlinkSpeed % 1f > 0.5f;

                foreach (ParticleSystemRenderer r in _renderers)
                {
                    r.enabled = blink;
                }
            }
        }

        public async UniTask PickupFrom(PlayerRef player)
        {
            var obj = Runner.GetPlayerObject(player);
            var container = obj.GetComponentInChildren<IJewelryContainer>();

            if (container == null) return;

            await _jewelryControl.PlayGetMove(obj.transform);

            container.PickUp(this);
        }

        public void PlayPickupEffect()
        {
            EffectSpawner effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
            effectSpawner.RequestPlayOneShotEffect(_jewelryParams.PickupEffectType, transform.position + _jewelryParams.PickupEffectOffset, transform.rotation);
        }

        public void PlaySpawnEffect()
        {
            if (!_spawnEffect || !_jewelryEffect || !_jewelryControl)
            {
                Debug.LogWarning("SpawnEffectの再生に失敗しました", this);
                return;
            }

            // スポーン前に移動しないように
            _jewelryControl.Freeze();

            // スポーンエフェクトを再生
            IsSpawnEffectPlaying = true;
            SpawnEffectTimer = TickTimer.CreateFromSeconds(Runner, _spawnTimeOffset);
        }

        private void OnChangedIsSpawnEffectPlaying()
        {
            // スポーンエフェクトを再生開始
            if (IsSpawnEffectPlaying)
            {
                _spawnEffect.Play(true);
                // 宝石そのものは非表示にする
                _jewelryEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            // スポーンエフェクトの再生完了後に宝石を表示
            else
            {
                _jewelryEffect.Play(true);
            }
        }
    }
}
