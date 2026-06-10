using System.Collections.Generic;

namespace September.InGame.Common.Stats
{
    public class IngameBuildRuntime
    {
        /// <summary>現在の進捗</summary>
        float _progressValue;
        /// <summary>ビルドシステムのパラメータテーブル</summary>
        readonly Queue<BuildConditionParams> _buildParamsQueue = new();
        public BuildConditionParams CurrentuBuildValue => _buildParamsQueue.Count > 0 ? _buildParamsQueue.Peek() : new BuildConditionParams();

        public IngameBuildRuntime(BuildDefinition definition)
        {
            foreach (var buildParams in definition.BuildTable)
            {
                _buildParamsQueue.Enqueue(buildParams);
            }
        }

        /// <summary>
        /// 現在までの進捗を更新するメソッド
        /// </summary>
        /// <param name="progressValue">更新に使用する値</param>
        /// <returns>ビルド更新が行われたかどうか</returns>
        public bool AddProgress(float progressValue = 1)
        {
            // 現在の進捗の更新
            _progressValue += progressValue;

            if (_buildParamsQueue.Count <= 0) return false;
            // 進捗が条件を満たしているか判定
            var nextBuildParams = _buildParamsQueue.Peek();
            if (nextBuildParams.BuildCondition != BuildRouteConditionType.MaxBuild
                && nextBuildParams.RequiredValue != -1
                && _progressValue >= nextBuildParams.RequiredValue)
            {
                // 条件を満たしていたらビルドレベルアップ
                _buildParamsQueue.Dequeue();
                return true;
            }

            return false;
        }
    }
}
