using InGame.Player;
using September.Common;
using UnityEngine;

namespace September
{
    public class BotDataBase : MonoBehaviour
    {
        public static BotDataBase Instance;
        private GameObject[] _interactObjects;
        private PlayerManager[] _playerManagers;

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

        public PlayerManager GetNearbyPlayer(GameObject myObject)
        {
            if(_playerManagers == null)
            {
                SetPlayerManager();
            }

            float minDistance = float.MaxValue;
            PlayerManager player = null;
            foreach (var p in _playerManagers)
            {
                if (p == null || p.gameObject == myObject || p.IsStun)
                    continue;

                float dis = (myObject.transform.position - p.transform.position).sqrMagnitude;
                if (minDistance > dis)
                {
                    minDistance = dis;
                    player = p;
                }
            }

            return player;
        }

        private void SetPlayerManager()
        {
            _playerManagers = new PlayerManager[PlayerDatabase.Instance.PlayerObjectDic.Count];
            int index = 0;
            foreach (var kv in PlayerDatabase.Instance.PlayerObjectDic)
            {
                _playerManagers[index] = kv.Value.GetComponent<PlayerManager>();
                index++;
            }
        }
    }
}
