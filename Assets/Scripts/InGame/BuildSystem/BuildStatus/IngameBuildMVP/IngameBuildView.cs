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
            stats.TrySetStatValue(_currentStatType, _currentBuildValue);
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
        /// <param name="statType">ステータスの種類</param>
        public void VisualizeBuild(bool upgraded, float param, StatType statType)
        {
            _currentBuildValue = param;
            if (upgraded)
            {
#if UNITY_EDITOR
                Debug.Log("ビルドレベルアップ");
#endif
            }
        }
    }
}
