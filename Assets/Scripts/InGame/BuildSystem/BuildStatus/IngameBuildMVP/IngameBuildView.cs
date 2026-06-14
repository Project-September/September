using UnityEngine;

namespace September.InGame.Common.Stats
{
    /// <summary>
    /// ビルドシステムのベースクラス
    /// PlayerStatus.csにアサインしないと起動しない
    /// </summary>
    public class IngameBuildView : MonoBehaviour
    {
        /// <summary>
        /// ビルドした時の描画メソッド
        /// </summary>
        /// <param name="upgraded">ビルドしたかどうか</param>
        /// <param name="param">現在のビルド状況</param>
        public void VisualizeBuild(bool upgraded, float param)
        {
            if (upgraded)
            {
#if UNITY_EDITOR
                var root = transform.root;
                if (root != null)
                {
                    Debug.Log($"{root.name} : ビルドレベルアップ\n 上昇 => {param}");
                }
#endif
            }
        }
    }
}
