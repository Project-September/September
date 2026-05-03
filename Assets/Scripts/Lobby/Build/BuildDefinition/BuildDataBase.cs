using UnityEngine;

namespace September.Lobby
{
    public abstract class BuildDataBase : ScriptableObject
    {
        [Header("ビルドルートのラベル")]
        [SerializeField] BuildType _buildType;
        [Header("ビルドルートの名前\n画面表示用")]
        [SerializeField] string _buildName;
        [Header("ビルドルートの説明")]
        [SerializeField, TextArea] string _buildInfo;
        [Header("ビルドルートの画像\n画面表示用")]
        [SerializeField] Sprite _buildSprite;

        public BuildType BuildType => _buildType;
        public string BuildName => _buildName;
        public string BuildInfo => _buildInfo;
        public Sprite BuildSprite => _buildSprite;

        /// <summary>ビルドクラス生成メソッド</summary>
        /// <returns>生成したビルドクラス</returns>
        public abstract IBuild CreateBuildInstance();
    }
}
