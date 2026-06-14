using System;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    public class BuildEffector : StatsEffectorBase
    {
        StatType _statType;
        float _currentProgress;
        bool _initialized;
        bool _isEnable;
        public bool IsEnable => _isEnable;

        event Action<float> _onUpdateBuild;
        event Action _onEnableBuild;
        public Action OnUpdateBuild(Action<float> act)
        {
            _onUpdateBuild += act;
            return () => _onUpdateBuild -= act;
        }
        public Action OnEnableBuild(Action act)
        {
            _onEnableBuild += act;
            return () => _onEnableBuild -= act;
        }

        /// <summary>
        /// 初期化メソッド
        /// </summary>
        /// <param name="statType">このクラスが担当するステータス</param>
        public void Init(StatType statType)
        {
            _statType = statType;
            _initialized = true;
        }

        public override void Apply(ref StatsContainer stats)
        {
            if (!_initialized || !_isEnable) return;
            // テスト用だが毎フレーム出るものかつ書き直すのが大変なのでコメントアウトで残しておく
            // Debug.Log($"{name} : {_statType} => {_currentProgress}");
            stats.TrySetStatValue(_statType, _currentProgress);
        }

        /// <summary>
        /// プレイヤーが選択したビルドルートに応じて有効化するメソッド
        /// </summary>
        public void TrySetEnableBuild()
        {
            _isEnable = true;
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
        /// パラメータを更新するメソッド
        /// </summary>
        /// <param name="upgraded">アップグレードしたかどうか</param>
        /// <param name="value">現在の数値</param>
        public void UpdateBuild(bool upgraded, float value)
        {
            if (upgraded) _currentProgress = value;
        }
    }
}
