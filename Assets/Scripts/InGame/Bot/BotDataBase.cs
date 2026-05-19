using Fusion;
using September.Common;
using UnityEngine;

namespace September
{
    public class BotDataBase : MonoBehaviour
    {
        public static BotDataBase Instance;
        private GameObject[] _interactObjects;

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public void Start()
        {
            //インタラクトオブジェクトを入れる
        }

        public NetworkObject GetNearbyPlayer(NetworkObject myObject)
        {
            float minDistance = float.MaxValue;
            NetworkObject player = null;
            foreach (var kv in PlayerDatabase.Instance.PlayerObjectDic)
            {
                var target = kv.Value;

                if (target == null || target == myObject)
                    continue;

                float dis = (myObject.transform.position - target.transform.position).sqrMagnitude;
                if (minDistance > dis)
                {
                    minDistance = dis;
                    player = target;
                }
            }

            return player;
        }
    }
}
