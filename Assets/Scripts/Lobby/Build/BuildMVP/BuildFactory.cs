using September.Common;
using UnityEngine;

namespace September.Lobby
{
    [CreateAssetMenu(fileName = "BuildFactory", menuName = "Build/BuildFactory")]
    public class BuildFactory : ScriptableObject
    {
        [SerializeField] BuildDatas _buildDatas;

        /// <summary>
        /// Build選択オブジェクトを作成するメソッド
        /// </summary>
        /// <param name="view">Buildの表示</param>
        /// <returns>Presenterを返す</returns>
        public BuildPresenter CreateBuild(BuildView view)
        {
            if (view == null || _buildDatas == null) return null;
            return new BuildPresenter(_buildDatas, view);
        }
    }
}
