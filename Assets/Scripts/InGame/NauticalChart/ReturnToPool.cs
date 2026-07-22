using UnityEngine;
using UnityEngine.Pool;

namespace September
{
    /// <summary>
    /// 
    /// </summary>
    [RequireComponent(typeof(GameObject))]
    public class ReturnToPool : MonoBehaviour
    {
        public GameObject system;
        public IObjectPool<GameObject> pool;

        void Start ()
        {

        }
    }
}
