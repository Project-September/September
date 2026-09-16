using Fusion;
using InGame.Jewelry;
using InGame.Player.Ult;
using September.InGame.Common.Stats;
using UnityEngine;

namespace InGame.Player.Takamura.Mimic
{
    /// <summary>Prefab交換をまたいで維持するプレイヤー状態。</summary>
    public readonly struct PlayerTransformationSnapshot
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 Velocity;
        public readonly int Health;
        public readonly float Stamina;
        public readonly NetworkBool IsInvincible;
        public readonly int UltConsumedScore;
        public readonly int[] JewelryQuantities;

        private PlayerTransformationSnapshot(
            Vector3 position,
            Quaternion rotation,
            Vector3 velocity,
            int health,
            float stamina,
            NetworkBool isInvincible,
            int ultConsumedScore,
            int[] jewelryQuantities)
        {
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            Health = health;
            Stamina = stamina;
            IsInvincible = isInvincible;
            UltConsumedScore = ultConsumedScore;
            JewelryQuantities = jewelryQuantities;
        }

        /// <summary>
        /// プレイヤーの現在情報を保存したデータを生成する静的メソッド
        /// </summary>
        /// <param name="player">情報を保存したいプレイヤー</param>
        /// <returns>保存したデータ</returns>
        public static PlayerTransformationSnapshot Capture(NetworkObject player)
        {
            var rigidbody = player.GetComponent<Rigidbody>();
            var status = player.GetComponent<PlayerStatus>();
            var health = player.GetComponent<PlayerHealth>();
            var ultCondition = player.GetComponent<UltCondition>();
            var jewelryRuntime = player.GetComponentInChildren<PlayerJewelryRuntime>(true);

            return new PlayerTransformationSnapshot(
                player.transform.position,
                player.transform.rotation,
                rigidbody ? rigidbody.linearVelocity : Vector3.zero,
                status ? status.CurrentHealth : 0,
                status ? status.CurrentStamina : 0f,
                health && health.IsInvincible,
                ultCondition ? ultCondition.ConsumedScore : 0,
                jewelryRuntime ? jewelryRuntime.CaptureJewelryQuantities() : null);
        }

        /// <summary>
        /// 擬態前の情報（PlayerTransformationSnapshot）を擬態オブジェクトに反映するメソッド
        /// </summary>
        /// <param name="player">擬態オブジェクト</param>
        public void ApplyTo(NetworkObject player)
        {
            player.transform.SetPositionAndRotation(Position, Rotation);

            var rigidbody = player.GetComponent<Rigidbody>();
            if (rigidbody)
                rigidbody.linearVelocity = Velocity;

            var status = player.GetComponent<PlayerStatus>();
            if (status)
            {
                status.SetBaseValue(StatType.Health, Mathf.Min(Health, status.MaxHealth));
                status.SetBaseValue(StatType.Stamina, Mathf.Min(Stamina, status.MaxStamina));
            }

            var health = player.GetComponent<PlayerHealth>();
            if (health)
                health.IsInvincible = IsInvincible;

            // 擬態ULTで消費済みになったスコア基準を新しいPrefabへ引き継ぐ。
            // これがないと新しいUltConditionのPrevScoreが0になり、
            // 擬態先のULTが満タンとして再発動できてしまう。
            var ultCondition = player.GetComponent<UltCondition>();
            if (ultCondition)
                ultCondition.RestoreConsumedScore(UltConsumedScore);

            // PlayerJewelryRuntimeの初期化はSpawnの1フレーム後に行われるため、
            // 初期化前なら復元予約として保持し、キャラクター既定値による上書きを防ぐ。
            var jewelryRuntime = player.GetComponentInChildren<PlayerJewelryRuntime>(true);
            if (jewelryRuntime)
                jewelryRuntime.RestoreJewelryQuantities(JewelryQuantities);
        }
    }
}
