using System.Collections;
using CRISound;
using Fusion;
using UnityEngine;

namespace InGame.Player.Hatano
{
    /// <summary>
    /// 武器のソケットを変更する
    /// </summary>
    public class HatanoWeaponController : NetworkBehaviour
    {
        [Header("ソケット（ロケット）")]
        [SerializeField] private Transform _rocketSocketRoot;
        [SerializeField] private Transform _rocketSocketBody;
        [SerializeField] private Transform _rocketSocketHand;
        [Header("ソケット（二丁拳銃）")]
        [SerializeField] private Transform[] _doubleSocketBody;
        [SerializeField] private Transform[] _doubleSocketHand;
        [Header("ソケット（レーザー銃）")]
        [SerializeField] private Transform _laserSocketHip;
        [SerializeField] private Transform _laserSocketHand;
        [Header("武器（Prefab）")]
        [SerializeField] private Transform _rocketPrefabTransform;
        [SerializeField] private Transform[] _doublePrefabTransform;
        [SerializeField] private Transform _laserPrefabTransform;

        [Header("二丁拳銃エフェクト（未設定なら再生しない）")]
        [SerializeField] private GameObject _muzzleFlashPrefab;
        [SerializeField] private GameObject _bulletTrailPrefab;
        [SerializeField] private GameObject _bulletHitPrefab;
        [SerializeField, Min(0.01f)] private float _bulletTrailSpeed = 100f;
        [SerializeField, Min(0.01f)] private float _effectLifetime = 2f;

        // 命中判定は権限側で行い、見た目には確定した終点だけを渡す。
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_PlayGunShot(Vector3 leftOrigin, Vector3 leftEnd, Vector3 leftNormal, bool leftHit,
            Vector3 rightOrigin, Vector3 rightEnd, Vector3 rightNormal, bool rightHit)
        {
            PlayBulletEffects(leftOrigin, leftEnd, leftNormal, leftHit);
            PlayBulletEffects(rightOrigin, rightEnd, rightNormal, rightHit);
            PlaySound(SoundCues.SE.Hatano_Shoot, (leftOrigin + rightOrigin) * 0.5f);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_PlayRocketReadySound()
        {
            PlaySound(SoundCues.SE.Hatano_Ult_1, transform.position);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_PlayRocketFireSound(Vector3 position)
        {
            PlaySound(SoundCues.SE.Hatano_Ult_2, position);
        }

        private static void PlaySound(CueData cue, Vector3 position)
        {
            if (CuePlayAtomExPlayer.Instance.IsReady)
                CRIAudio.PlaySE(position, cue.Sheet, cue.Name);
        }

        private void PlayBulletEffects(Vector3 origin, Vector3 end, Vector3 normal, bool hit)
        {
            var direction = end - origin;
            var rotation = direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction) : transform.rotation;
            SpawnShotEffect(_muzzleFlashPrefab, origin, rotation);
            // hitscan のダメージ判定と同じタイミングで着弾を表示する。
            if (hit)
                SpawnShotEffect(_bulletHitPrefab, end,
                    normal.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(normal) : rotation);

            if (_bulletTrailPrefab != null)
                StartCoroutine(PlayBulletTrail(origin, end, rotation));
        }

        private void SpawnShotEffect(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return;
            var effect = Instantiate(prefab, position, rotation);
            Destroy(effect, Mathf.Max(0.01f, _effectLifetime));
        }

        private IEnumerator PlayBulletTrail(Vector3 origin, Vector3 end, Quaternion rotation)
        {
            var trail = Instantiate(_bulletTrailPrefab, origin, rotation);
            var duration = Vector3.Distance(origin, end) / Mathf.Max(0.01f, _bulletTrailSpeed);
            // キャラクター消滅でコルーチンが停止しても、生成物は必ず破棄する。
            Destroy(trail, duration + Mathf.Max(0.01f, _effectLifetime));
            var elapsed = 0f;
            while (trail != null && elapsed < duration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (trail != null)
                    trail.transform.position = Vector3.Lerp(origin, end, Mathf.Clamp01(elapsed / duration));
            }

            if (trail == null) yield break;
            foreach (var particle in trail.GetComponentsInChildren<ParticleSystem>())
                particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            foreach (var renderer in trail.GetComponentsInChildren<TrailRenderer>())
                renderer.emitting = false;
        }

        public override void Spawned()
        {
            // Prefab 保存時の姿勢に依存せず、開始時の装備をソケットへ配置する。
            AttachRocketBody();
            var status = GetComponentInParent<HatanoAbilityStatusManagement>();
            if (status != null && status.AbilityStatus == HatanoAbilityStatus.LaserGun)
            {
                AttachLaserGunHand();
                AttachDoubleGunBody();
            }
            else
            {
                AttachDoubleGunHand();
                AttachLaserGunHip();
            }
        }
        
        #region 二丁拳銃

        public void AttachDoubleGunBody()
        {
            AttachSocket(_doublePrefabTransform[0], _doubleSocketBody[0]);
            AttachSocket(_doublePrefabTransform[1], _doubleSocketBody[1]);
        }

        public void AttachDoubleGunHand()
        {
            AttachSocket(_doublePrefabTransform[0], _doubleSocketHand[0]);
            AttachSocket(_doublePrefabTransform[1], _doubleSocketHand[1]);
        }

        #endregion
        
        #region レーザー銃

        public void AttachLaserGunHip()
        {
            AttachSocket(_laserPrefabTransform, _laserSocketHip);
        }

        public void AttachLaserGunHand()
        {
            AttachSocket(_laserPrefabTransform, _laserSocketHand);
        }

        #endregion
        
        #region ロケットランチャー

        public void AttachRocketRoot()
        {
            AttachSocket(_rocketPrefabTransform, _rocketSocketRoot);
        }

        public void AttachRocketBody()
        {
            AttachSocket(_rocketPrefabTransform, _rocketSocketBody);
        }

        public void AttachRocketSocketHand()
        {
            AttachSocket(_rocketPrefabTransform, _rocketSocketHand);
        }

        #endregion

        /// <summary>
        /// ULT終了後の武器ソケット変更
        /// </summary>
        /// <param name="status">現在のアビリティ</param>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_UltEndAttachSocket(HatanoAbilityStatus status)
        {
            AttachRocketBody();
            if (status == HatanoAbilityStatus.DoubleBarreledGun)
            {
                AttachDoubleGunHand();
                AttachLaserGunHip();
            }
            else // レーザー銃
            {
                AttachLaserGunHand();
                AttachDoubleGunBody();
            }
        }
        
        private void AttachSocket(Transform prefab, Transform socket)
        {
            prefab.SetParent(socket);
            
            prefab.localPosition = Vector3.zero;
            prefab.localRotation = Quaternion.identity;
            prefab.localScale = Vector3.one;
        }
    }
}
