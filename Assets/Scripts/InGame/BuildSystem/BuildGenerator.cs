using Cysharp.Threading.Tasks;
using Fusion;
using September.Common;
using September.InGame.Common.Stats;
using UnityEngine;

namespace September.InGame.Common
{
    /// <summary>
    /// ビルドルートの機能を生成するクラス
    /// </summary>
    public class BuildGenerator : NetworkBehaviour
    {
        [Header("ビルドルートの機能"), SerializeField] BuildSystem[] _builds;

        void GenerateBuild(SessionPlayerData player)
        {

        }
    }
}
