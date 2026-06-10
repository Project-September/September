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
        BuildConditionParams _currentConditionParams;
        StatType _currentStatType;
        // MVPを使う都合上、現在のビルド状況をRuntimeから受け取って直接使えないため
        // 一時的に保存するための変数を定義

        bool _isEnable;
        public bool IsEnable => _isEnable;

        event Action<BuildRouteType> _onEnableBuild;
        event Action<float> _onUpdateBuild;
        public Action OnEnableBuild(Action<BuildRouteType> act)
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
            switch (_currentConditionParams.CalculateType)
            {
                case BuildCalculateType.Add:
                    // 獲得ビルドを加算
                    stats.TryAddStatValue(_currentStatType, _currentConditionParams.CurrentBuildParam);
                    break;
                case BuildCalculateType.Multiply:
                    if (stats.TryGetStatValue(_currentStatType, out var value))
                    {
                        stats.TrySetStatValue(_currentStatType, value * _currentConditionParams.CurrentBuildParam);
                    }
                    break;
            }
        }

        /// <summary>
        /// プレイヤーが選択したビルドルートに応じて有効化するメソッド
        /// </summary>
        /// <param name="build">プレイヤーが選択したビルドルート</param>
        public void TrySetEnableBuild(BuildRouteType build)
        {
            _onEnableBuild?.Invoke(build);
        }

        /// <summary>
        /// ビルド状況の更新メソッド
        /// </summary>
        /// <param name="value">更新する値</param>
        public void UpdateBuild(float value = 1)
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
        public void VisualizeBuild(bool upgraded, BuildConditionParams param, StatType statType)
        {
            _currentConditionParams = param;
            if (upgraded)
            {
#if UNITY_EDITOR
                Debug.Log("ビルドレベルアップ");
#endif
            }
        }
    }
}
