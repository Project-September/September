using CRISound;
using UnityEngine;

namespace September.Common
{
    public class CRIListenerManager : MonoBehaviour
    {
        [SerializeField] private Transform _follow;   // MainCamera をアタッチ（実行時に差し替え可）

        private static CRIListenerManager _instance;


        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LateUpdate()
        {
            if (_follow == null) return;

            // 3DプレイヤーのListenerがあるときのみ
            var sePlayer = CuePlayAtomExPlayer.Instance.Player(SoundType.SE) as CuePlayAtomExPlayer.SEPlayerWith3D;
            if (sePlayer == null || sePlayer.Listener == null) return;

            // 位置更新
            var pos = _follow.position;
            sePlayer.Listener.SetPosition(pos.x, pos.y, pos.z);

            // 角度更新
            var forward = _follow.forward.normalized;
            var up = _follow.up.normalized;
            sePlayer.Listener.SetOrientation(forward.x, forward.y, forward.z, up.x, up.y, up.z);
            sePlayer.Listener.Update();
        }

        /// <summary>
        /// サウンドのListenerをオブジェクトにアタッチする
        /// 実行中にカメラが切り替わる場合などに使用
        /// </summary>
        /// <param name="cam"></param>
        public void Attach(Transform obj) => _follow = obj;
    }
}