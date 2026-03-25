using System;
using Fusion;
using UnityEngine;

namespace  InGame.Player.Ability
{
    /// <summary>
    /// アビリティ実行条件を定義するインターフェース
    /// プレイヤーの入力やゲーム状態を元に、どのアビリティをいつ発動するかの条件を判定する
    /// </summary>
    public interface IAbilityExecuteCondition
    {
        /// <summary>
        /// 対象となるアビリティのクラス名（例: "AbilityNormalAttack"）
        /// </summary>
        public string TargetAbilityName { get; }

        /// <summary>
        /// 条件が満たされているかを判定する
        /// </summary>
        /// <param name="context">条件判定に必要な情報（オーナー、アビリティ参照、ボタン入力）</param>
        /// <returns>true: 条件を満たす（アビリティを開始）、false: 条件を満たさない</returns>
        public bool IsConditionMatch(in TriggerEventContext context);
    }

    /// <summary>
    /// 条件判定に必要な情報を保持する構造体
    /// PlayerAbilityManagerから条件クラスに渡される
    /// </summary>
    public readonly struct TriggerEventContext
    {
        /// <summary>アビリティを実行するプレイヤーのGameObject</summary>
        public GameObject Owner { get; }
        /// <summary>対象アビリティの参照（フェーズ確認などに使用）</summary>
        public AbilityBase AbilityRef { get; }
        /// <summary>現在のフレームのボタン入力状態</summary>
        public NetworkButtons CurrentButtons { get; }
        /// <summary>前フレームのボタン入力状態（エッジ検出に使用）</summary>
        public NetworkButtons PreviousButtons { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="owner">プレイヤーのGameObject</param>
        /// <param name="abilityRef">対象アビリティの参照</param>
        /// <param name="currentButtons">現在のボタン入力</param>
        /// <param name="previousButtons">前フレームのボタン入力</param>
        public TriggerEventContext(GameObject owner, AbilityBase abilityRef, NetworkButtons currentButtons, NetworkButtons previousButtons)
        {
            Owner = owner;
            AbilityRef = abilityRef;
            CurrentButtons = currentButtons;
            PreviousButtons = previousButtons;
        }
    }
}
