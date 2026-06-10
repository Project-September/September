using Fusion;
using September.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    /// <summary>
    /// ビルドルートの機能を生成するクラス
    /// </summary>
    public class BuildGenerator : NetworkBehaviour
    {
        [Serializable]
        class BuildGenerateInfo
        {
            [SerializeField] BuildRouteType _buildRouteType;
            [SerializeField] IngameBuildView _view;
            [SerializeField] IngameBuildFactory _factory;

            public BuildRouteType BuildRouteType => _buildRouteType;
            public IngameBuildView View => _view;

            public void GenerateBuild(BuildRouteType build)
            {
                _factory?.CreateBuild(_view);
                if (_buildRouteType == build) _view?.TrySetEnableBuild();
            }
        }

        [Header("ビルドルートの機能"), SerializeField] BuildGenerateInfo[] _builds;
        readonly Dictionary<BuildRouteType, IngameBuildView> _buildDict = new();

        /// <summary>
        /// プレイヤーが選択したビルドルートに応じてシステムを有効化するメソッド
        /// </summary>
        /// <param name="buildType">選択したビルドルート</param>
        public void GenerateBuild(BuildRouteType buildType)
        {
            foreach (var build in _builds)
            {
                if (build == null) continue;
                build.GenerateBuild(buildType);
                _buildDict[build.BuildRouteType] = build.View;
            }
        }

        /// <summary>
        /// ビルドするメソッド
        /// </summary>
        /// <param name="buildType">ビルドルート</param>
        /// <param name="value">ビルド量</param>
        public void UpdateBuild(BuildRouteType buildType, float value = 1)
        {
            if (!_buildDict.TryGetValue(buildType, out var build)) return;
            build.UpdateBuild(value);
        }

        /// <summary>
        /// ビルドシステムが有効かどうかを返すメソッド
        /// </summary>
        /// <param name="buildType">有効かどうかを調べるビルドルート</param>
        /// <returns>有効かどうか</returns>
        public bool TryGetBuildEnable(BuildRouteType buildType)
        {
            return _buildDict.TryGetValue(buildType, out var build) && build.IsEnable;
        }
    }
}
