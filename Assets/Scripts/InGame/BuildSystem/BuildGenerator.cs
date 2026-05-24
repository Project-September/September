using Fusion;
using September.Common;
using System.Collections.Generic;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    /// <summary>
    /// ビルドルートの機能を生成するクラス
    /// </summary>
    public class BuildGenerator : NetworkBehaviour
    {
        [Header("ビルドルートの機能"), SerializeField] BuildSystem[] _builds;
        readonly Dictionary<BuildType, BuildSystem> _buildDict = new();

        /// <summary>
        /// プレイヤーが選択したビルドルートに応じてシステムを有効化するメソッド
        /// </summary>
        /// <param name="buildType">選択したビルドルート</param>
        public void GenerateBuild(BuildType buildType)
        {
            foreach (var build in _builds)
            {
                if (build == null) continue;
                build.Init();
                _buildDict.Add(build.BuildType, build);
                build.TryEnableBuild(buildType);
            }
        }

        /// <summary>
        /// ビルドするメソッド
        /// </summary>
        /// <param name="buildType">ビルドルート</param>
        /// <param name="value">ビルド量</param>
        public void UpdateBuild(BuildType buildType, float value)
        {
            if (!_buildDict.TryGetValue(buildType, out var build)) return;
            build.UpdateBuild(value);
        }

        /// <summary>
        /// ビルドシステムが有効かどうかを返すメソッド
        /// </summary>
        /// <param name="buildType">有効かどうかを調べるビルドルート</param>
        /// <returns>有効かどうか</returns>
        public bool TryGetBuildEnable(BuildType buildType)
        {
            return _buildDict.TryGetValue(buildType, out var build) && build.IsEnable;
        }
    }
}
