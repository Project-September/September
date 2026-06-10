using System;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    /// <summary>ビルドルートごとのパラメータテーブルを定義するクラス</summary>
    [CreateAssetMenu(fileName = "BuildDefinition", menuName = "Build/BuildDefinition")]
    public class BuildDefinition : ScriptableObject
    {
        [SerializeField] BuildRouteType _buildType;
        [Header("ビルドシステムの配列\nElement0は未ビルド（ステータス上昇していないとき）の状態"), SerializeField] BuildConditionParams[] _buildTable;

        public BuildRouteType BuildType => _buildType;
        public BuildConditionParams[] BuildTable => _buildTable;
    }

    /// <summary>現在のビルド状況におけるパラメータ群</summary>
    [Serializable]
    public struct BuildConditionParams
    {
        [Header("ビルド状況"), SerializeField] BuildRouteConditionType _buildCondition;
        [Header("パラメータの計算方法"), SerializeField] BuildCalculateType _calculateType;
        [Header("現在のビルドパラメータ\n"), SerializeField] float _currentBuildParam;
        [Header("次のビルドまでの条件値\nこの値に到達したら次のレベルに進む\n最大レベルの場合-1を入れておくといい"), SerializeField, Min(-1), Tooltip("攻撃力 : 回\nスピード : m\netc...")]
        float _requiredValue;

        public BuildRouteConditionType BuildCondition => _buildCondition;
        public BuildCalculateType CalculateType => _calculateType;
        public float CurrentBuildParam => _currentBuildParam;
        public float RequiredValue => _requiredValue;
    }
}
