using September.InGame.Common.Stats;
using UnityEngine;

namespace InGame.Player
{
    /// <summary>
    /// プレイヤーステータスの定義データを格納するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerParameter", menuName = "Scriptable Objects/Player/PlayerParameter")]
    public class PlayerParameter : ScriptableObject
    {
        [SerializeField] Stat[] _stats;
        
        public StatsContainer GetStats()
        {
            return new StatsContainer(_stats);
        }
    }
}
