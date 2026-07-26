using Fusion;
using InGame.Common;
using InGame.Health;
using InGame.Player.Ability.Effect;
using September.Common;
using September.InGame.Effect;
using UnityEngine;

namespace InGame.Player.Ability
{
    public class HatanoUlt : AbilityUltBase
    {
        [SerializeField] private AimCameraController _aimCameraController;
        [Header("必殺技終了時間"), SerializeField] private float _duration;
        [Header("ロケランの弾"), SerializeField] private GameObject _bulletPrefab;
        [Header("弾の速さ"), SerializeField] private float _bulletSpeed;
        [SerializeField] private EffectType _predictedLocation;
        [SerializeField] private EffectType _impact;
        [Header("構えアニメーション"), SerializeField] private AnimationClip _stanceAnimationClip;
        [Header("必殺技の効果設定")]
        [Header("攻撃範囲"), SerializeField] private float _rocketLauncherRadius;
        [Header("射程距離"), SerializeField] private float _shootingDistance;
        [Header("muzzle"), SerializeField] private Transform _muzzle;
        [SerializeField] private int _damage;
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private float _offSet = 0.05f;
        
        private AnimationClipPlayer _animationClipPlayer;
        private EffectSpawner _effectSpawner;
        private NetworkBool _isShoot;
        
        private string _idPredictedLocation = "HatanoUltPredictedLocation";
        
        // TODO：クライアント側で着弾エフェクトと弾が表示されない
        
        protected override void OnCutInEnd()
        {
            // 構えアニメーションを再生
            if(_animationClipPlayer == null) _animationClipPlayer = Parameter.Owner.GetComponent<AnimationClipPlayer>();
            _animationClipPlayer.PlayClip(_stanceAnimationClip);
            
            // カメラの方向にプレイヤーを向ける
            _aimCameraController.RPC_AimCamera();
            
            // エフェクト生成
            _effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
            _effectSpawner?.RequestPlayLoopEffect(_idPredictedLocation, _predictedLocation, Vector3.zero, Quaternion.identity);
        }

        protected override void OnUpdateUlt(float deltaTime)
        {
            if (TimeSinceCutInEnd > _duration)
            {
                RequestEndAbility();
            }
            
            UpdateEffectPosition();
            // アニメーション終了後
            if (!_isShoot && TimeSinceCutInEnd >= _stanceAnimationClip.length)
            {
                _isShoot = true;
                RPC_Shooting();
            }
        }

        protected override void OnEndUlt()
        {
            _animationClipPlayer.StopClip(_stanceAnimationClip);
            _aimCameraController.RPC_NormalCamera();
            _effectSpawner?.StopEffect(_idPredictedLocation);
            _isShoot = false;
        }
        
        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_Shooting()
        {
            LauncherShootingDetection();
        }

        /// <summary>
        /// エフェクトの位置を更新する
        /// </summary>
        private void UpdateEffectPosition()
        {
            var aimOri = _aimCameraController.AimOrigin;
            var aimDir = _aimCameraController.AimDirection;
            
            Debug.DrawRay(aimOri, aimDir * _shootingDistance, Color.red, 5);

            Vector3 targetPos;
            Vector3 normal;
            
            // カメラの前方方向にRayを飛ばす
            if (Physics.Raycast(aimOri, aimDir, out var hit, _shootingDistance, _layerMask))
            {
                targetPos =  hit.point;
                normal = hit.normal;
            }
            else // hitしなかったら射程距離の位置
            {
                targetPos = aimOri + aimDir * _shootingDistance;
                normal = Vector3.up;
            }
            
            var pos = targetPos + normal * _offSet;
            var rot = Quaternion.FromToRotation(Vector3.up, normal);
            _effectSpawner?.UpdateEffect(_idPredictedLocation, pos, rot);
        }
        
        /// <summary>
        /// 着弾位置を決定
        /// </summary>
        private void LauncherShootingDetection()
        {
            var aimOri = _aimCameraController.AimOrigin;
            var aimDir = _aimCameraController.AimDirection;
            
            Debug.DrawRay(aimOri, aimDir * _shootingDistance, Color.red, 5);

            // カメラの前方方向にRayを飛ばす
            if (Physics.Raycast(aimOri, aimDir, out var hit, _shootingDistance, _layerMask))
            {
                GenerateRocket(hit.point);
            }
            else // hitしなかったら射程距離の位置
            {
                GenerateRocket(aimOri + aimDir * _shootingDistance);
            }
        }

        /// <summary>
        /// ロケットを生成
        /// </summary>
        private void GenerateRocket(Vector3 position)
        {
            if(!Parameter.Owner.HasStateAuthority) return;
            // マズル位置に生成し、移動させる
            var rocket = Runner.Spawn(_bulletPrefab, _muzzle.position, Quaternion.identity);
            if (rocket.TryGetComponent<RocketBullet>(out var rocketBullet))
            {
                rocketBullet.Initialization(position, _bulletSpeed, () => RocketLauncherRadius(position));
            }
        }

        /// <summary>
        /// ロケットランチャーの攻撃処理
        /// </summary>
        /// <param name="targetPos">ヒットした場所</param>
        private void RocketLauncherRadius(Vector3 targetPos)
        {
            // 攻撃範囲内のオブジェクトを取得
            Collider[] radiusObjs = Physics.OverlapSphere(targetPos, _rocketLauncherRadius);
            var playerInput = Parameter.Owner.InputAuthority;
            foreach (var obj in radiusObjs)
            {
                var damageable = obj.GetComponentInParent<IDamageable>();
                if(damageable == null) continue;
                
                // 自身に当たっていたらスキップ
                if(damageable.OwnerPlayerRef == playerInput) continue;
                
                // ダメージ処理
                var damage = _damage;
                var hitData = new HitData(HitActionType.Damage,
                    damage, playerInput, damageable.OwnerPlayerRef, null, damageable);
                damageable.TakeHit(ref hitData);
            }
            
            // 着弾エフェクトを再生
            _effectSpawner?.RequestPlayOneShotEffect(_impact, targetPos, Quaternion.identity);
        }
    }
}
