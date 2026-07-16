using System;
using UnityEngine;
using September.Common;

namespace September.InGame.Common.Stats
{
    /// <summary>ビルドルートごとのパラメータテーブルを定義するクラス</summary>
    [CreateAssetMenu(fileName = "BuildDefinition", menuName = "Build/BuildDefinition")]
    public class BuildDefinition : ScriptableObject
    {
        [SerializeField] BuildRouteType _buildType;
        [SerializeField] StatType _statType;
        [Header("ビルドシステムの配列\nElement0は未ビルド（ステータス上昇していないとき）の状態"), SerializeField] BuildConditionParams[] _buildTable;

        public BuildRouteType BuildType => _buildType;
        public StatType StatType => _statType;
        public BuildConditionParams[] BuildTable => _buildTable;
    }

    /// <summary>現在のビルド状況におけるパラメータ群</summary>
    [Serializable]
    public struct BuildConditionParams
    {
        [Header("現在のビルドパラメータ\n初期パラメータとの差分"), SerializeField] float _currentBuildParam;
        [Header("現在のビルドパラメータに到達するために必要な数値"),
            SerializeField, Min(-1), Tooltip("攻撃力 : 回\nスピード : m\netc...")]
        float _requiredValue;

        public float CurrentBuildParam => _currentBuildParam;
        public float RequiredValue => _requiredValue;
    }
}
