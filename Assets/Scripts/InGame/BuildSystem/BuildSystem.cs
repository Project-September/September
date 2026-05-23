using September.Common;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    public abstract class BuildSystem : StatsEffectorBase
    {
        [SerializeField] protected BuildDataBase _buildData;
        protected bool _enable = false;

        public BuildType BuildType => _buildData.BuildType;

        /// <summary>初期化メソッド</summary>
        /// <returns>初期化が完了したかどうか</returns>
        public abstract bool Init();

        /// <summary>
        /// このビルドを有効化するメソッド
        /// </summary>
        public void TryEnableBuild(BuildType build)
        {
            _enable = build == BuildType;
        }

        /// <summary>ビルドの状態を更新するメソッド</summary>
        public abstract void UpdateBuild(float value);
    }
}
