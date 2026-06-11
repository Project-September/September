using System;
using September.Common;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    /// <summary>
    /// ビルドシステムのベースクラス
    /// PlayerStatus.csにアサインしないと起動しない
    /// </summary>
    public class IngameBuildView : StatsEffectorBase
    {
        float _currentBuildValue;
        StatType _currentStatType;
        // MVPを使う都合上、現在のビルド状況をRuntimeから受け取って直接使えないため
        // 一時的に保存するための変数を定義

        bool _isEnable;
        public bool IsEnable => _isEnable;

        event Action _onEnableBuild;
        event Action<float> _onUpdateBuild;
        public Action OnEnableBuild(Action act)
        {
            _onEnableBuild += act;
            return () => _onEnableBuild -= act;
        }
        public Action OnUpdateBuild(Action<float> act)
        {
            _onUpdateBuild += act;
            return () => _onUpdateBuild -= act;
        }

        public override void Apply(ref StatsContainer stats)
        {
            stats.TrySetStatValue(_currentStatType, _currentBuildValue);
        }

        /// <summary>
        /// プレイヤーが選択したビルドルートに応じて有効化するメソッド
        /// </summary>
        public void TrySetEnableBuild()
        {
            _onEnableBuild?.Invoke();
        }

        /// <summary>
        /// ビルド状況の更新メソッド
        /// </summary>
        /// <param name="value">更新する値</param>
        public void UpdateBuild(float value)
        {
            _onUpdateBuild?.Invoke(value);
        }

        /// <summary>
        /// ビルドが有効化されたらこのクラスを動作するようにするメソッド
        /// </summary>
        public void EnableBuild()
        {
            _isEnable = true;
        }

        /// <summary>
        /// ビルドした時の描画メソッド
        /// </summary>
        /// <param name="upgraded">ビルドしたかどうか</param>
        /// <param name="param">現在のビルド状況</param>
        /// <param name="statType">ステータスの種類</param>
        public void VisualizeBuild(bool upgraded, float param, StatType statType)
        {
            _currentBuildValue = param;
            _currentStatType = statType;
            if (upgraded)
            {
#if UNITY_EDITOR
                var root = transform.root;
                if (root != null)
                {
                    Debug.Log($"{root.name} : ビルドレベルアップ\n{statType} 上昇 => {param}");
                }
#endif
            }
        }
    }
}
