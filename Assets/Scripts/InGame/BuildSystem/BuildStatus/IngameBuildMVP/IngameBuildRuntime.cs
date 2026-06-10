using September.Common;
using System;
using System.Collections.Generic;

namespace September.InGame.Common.Stats
{
    public class IngameBuildRuntime
    {
        BuildRouteType _buildRouteType;
        StatType _statType;
        bool _enabled;
        /// <summary>現在の進捗</summary>
        float _progressValue;
        /// <summary>ビルドシステムのパラメータテーブル</summary>
        readonly Queue<BuildConditionParams> _buildParamsQueue = new();

        event Action _onEnableBuild;
        event Action<bool, float, StatType> _onAddProgress;
        public Action OnEnableBuild(Action act)
        {
            _onEnableBuild = act;
            return () => _onEnableBuild -= act;
        }
        public Action OnAddProgress(Action<bool, float, StatType> act)
        {
            _onAddProgress += act;
            return () => _onAddProgress -= act;
        }

        public IngameBuildRuntime(BuildDefinition definition)
        {
            _enabled = false;
            _buildRouteType = definition.BuildType;
            _statType = _buildRouteType switch
            {
                BuildRouteType.AttackPower => StatType.AttackDamage,
                BuildRouteType.MoveSpeed => StatType.Speed,
                BuildRouteType.StunResistance => StatType.StunDurationMultiply,
                BuildRouteType.FastInteract => StatType.InteractDurationMultiply,
                _ => StatType.AttackDamage
            };

            foreach (var buildParams in definition.BuildTable)
            {
                _buildParamsQueue.Enqueue(buildParams);
            }
        }

        public void EnableBuild(BuildRouteType build)
        {
            if (build != _buildRouteType) return;
            _enabled = true;
            _onEnableBuild?.Invoke();
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
            if (nextBuildParams.BuildCondition != BuildRouteConditionType.MaxBuild
                && nextBuildParams.RequiredValue != -1
                && _progressValue >= nextBuildParams.RequiredValue)
            {
                // 条件を満たしていたらビルドレベルアップ
                _buildParamsQueue.Dequeue();
                upgrade = true;
            }

            // ビルドレベルアップしたら描画用に通知
            if (_buildParamsQueue.Count <= 0) return;
            _onAddProgress?.Invoke(upgrade, _buildParamsQueue.Peek().CurrentBuildParam, _statType);
        }
    }
}
