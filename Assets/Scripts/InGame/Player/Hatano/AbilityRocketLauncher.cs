using System;
using UnityEngine;

namespace InGame.Player.Ability
{
    [Serializable]
    public class AbilityRocketLauncher : AbilityBase
    {
        [Header("射撃のステート")] 
        [SerializeField] private ShootingStateType _shootingStateType;

        //クールダウン用のタイマー
        private float _cooldownTimer;

        protected override void OnStart()
        {
            //ステートを何もしてない状態にする
            _shootingStateType = ShootingStateType.None;
        }

        protected override void OnUpdate(float deltaTime)
        {
            //ロケランを構えている状態
            //構えた状態で入力があったら発射する
            //発射終わったら、インターバルの秒数を待つ
        }

        public override void OnUpdateLocal(float deltaTime, GameObject owner)
        {
            
        }
    }
}
