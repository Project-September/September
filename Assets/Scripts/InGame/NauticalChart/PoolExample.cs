using UnityEngine;
using UnityEngine.Pool;

namespace September
{
    public class PoolExample : MonoBehaviour
    {
        public enum PoolType
        {
            Stack,
            LinkedList
        }

        public PoolType poolType;

        public bool collectionCheck = true;
        public int maxPoolSize = 5;

        IObjectPool<GameObject> pool;

        public IObjectPool<GameObject> Pool
        {
            get
            {
                if (pool == null)
                {
                    switch (poolType == PoolType.Stack)
                    {
                        //case PoolType.Stack:
                        //    pool = new ObjectPool<GameObject>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, collectionCheck, 10, maxPoolSize);
                        //    break;
                        //case PoolType.LinkedList:
                        //    pool = new LinkedPool<GameObject>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, collectionCheck, maxPoolSize);
                        //    break;
                    }
                }
                return pool;
            }
        }

        /// <summary> 再利用するオブジェクトを生成する </summary>
        //private GameObject CreatePooledItem()
        //{
        //    var go = new GameObject("Pooled GameObject");
        //    var ps = go.AddComponent<GameObject>();
        //}
    }
}
