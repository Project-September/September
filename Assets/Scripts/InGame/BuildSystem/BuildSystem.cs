using September.Common;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    /// <summary>
    /// ビルドシステムのベースクラス
    /// PlayerStatus.csにアサインしないと起動しない
    /// </summary>
    public abstract class BuildSystem : StatsEffectorBase
    {
        [SerializeField] protected BuildDataBase _buildData;
        protected bool _isEnable = false;

        public bool IsEnable => _isEnable;

        public BuildType BuildType => _buildData.BuildType;

        /// <summary>初期化メソッド</summary>
        /// <returns>初期化が完了したかどうか</returns>
        public abstract bool Init();

        /// <summary>
        /// このビルドを有効化するメソッド
        /// </summary>
        public void TryEnableBuild(BuildType build)
        {
            _isEnable = build == BuildType;
        }

        /// <summary>ビルドの状態を更新するメソッド</summary>
        public abstract void UpdateBuild(float value);
    }

    /// <summary>
    /// ビルドシステムを作成するときに継承するクラス
    /// </summary>
    /// <typeparam name="TBuild">ビルドルートのScriptableObjectクラス</typeparam>
    /// <typeparam name="TBuildRuntime">ビルドルートのランタイムクラス</typeparam>
    public abstract class IngameBuildSystem<TBuild, TBuildRuntime> : BuildSystem where TBuild : BuildDataBase where TBuildRuntime : class, IBuild
    {
        protected TBuildRuntime _runtime;

        public sealed override bool Init()
        {
            if (_buildData == null) return false;
            if (_buildData is not TBuild) return false;
            _runtime = _buildData.CreateBuildInstance() as TBuildRuntime;
            if (_runtime == null) return false;
            return true;
        }
    }
}
