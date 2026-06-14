using System;
using System.Collections.Generic;

namespace September.InGame.Common.Stats
{
    public class IngameBuildRuntime
    {
        BuildDefinition _definition;
        bool _enabled;
        /// <summary>現在のビルドパラメータ</summary>
        float _currentParam;
        /// <summary>現在の進捗</summary>
        float _progressValue;
        /// <summary>ビルドシステムのパラメータテーブル</summary>
        readonly Queue<BuildConditionParams> _buildParamsQueue;

        event Action<StatType> _onEnableBuild;
        event Action<bool, float> _onAddProgress;
        public Action OnEnableBuild(Action<StatType> act)
        {
            _onEnableBuild += act;
            return () => _onEnableBuild -= act;
        }
        public Action OnAddProgress(Action<bool, float> act)
        {
            _onAddProgress += act;
            return () => _onAddProgress -= act;
        }

        public IngameBuildRuntime(BuildDefinition definition)
        {
            _definition = definition;
            _enabled = false;
            _buildParamsQueue = new Queue<BuildConditionParams>();
        }

        /// <summary>
        /// このビルドルートを有効化するメソッド
        /// </summary>
        public void EnableBuild()
        {
            _enabled = true;

            // ステータス更新に必要なStatTypeをEffectorに送信
            _onEnableBuild?.Invoke(_definition.StatType);

            // ビルドルートのパラメータテーブルを登録
            foreach (var buildParams in _definition.BuildTable)
            {
                _buildParamsQueue.Enqueue(buildParams);
            }

            // 有効な場合はステータスを初期化
            if (_buildParamsQueue.Count > 0) _currentParam = _buildParamsQueue.Dequeue().CurrentBuildParam;
            _onAddProgress?.Invoke(true, _currentParam);
        }

        /// <summary>
        /// 現在までの進捗を更新するメソッド
        /// </summary>
        /// <param name="progressValue">更新に使用する値</param>
        public void AddProgress(float progressValue)
        {
            if (!_enabled) return;

            var upgrade = false;
            // 現在の進捗の更新
            _progressValue += progressValue;

            if (_buildParamsQueue.Count <= 0) return;
            // 進捗が条件を満たしているか判定
            var nextBuildParams = _buildParamsQueue.Peek();
            if (_progressValue >= nextBuildParams.RequiredValue)
            {
                // 条件を満たしていたらビルドレベルアップ
                _currentParam = _buildParamsQueue.Dequeue().CurrentBuildParam;
                upgrade = true;
            }

            // ビルドレベルアップしたかどうかと現在のステータスを送信
            _onAddProgress?.Invoke(upgrade, _currentParam);
        }
    }
}
