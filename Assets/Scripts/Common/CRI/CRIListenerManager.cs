using CRISound;
using UnityEngine;

namespace September.Common
{
    public class CRIListenerManager : MonoBehaviour
    {
        [SerializeField] private Transform _follow;   // Player をアタッチ（実行時に差し替え可）
        [SerializeField] private Transform _camera;   // MainCamera をアタッチ（実行時に差し替え可）

        /// <summary> リスナーを探し、なければ生成する </summary>
        /// <returns></returns>
        public static CRIListenerManager GetOrCreateInLocalListener()
        {
            var found = FindFirstObjectByType<CRIListenerManager>();
            if (found) return found;

            var obj = new GameObject("Listener");
            var newManager = obj.AddComponent<CRIListenerManager>();
            DontDestroyOnLoad(obj);
            return newManager;
        }

        private void LateUpdate()
        {
            if (_follow == null) return;

            // 3DプレイヤーのListenerがあるときのみ
            var sePlayer = CuePlayAtomExPlayer.Instance.Player(SoundType.SE) as CuePlayAtomExPlayer.SEPlayerWith3D;
            if (sePlayer == null || sePlayer.Listener == null) return;

            // 位置更新 プレイヤー基準
            var pos = _follow.position;
            sePlayer.Listener.SetPosition(pos.x, pos.y, pos.z); // 距離減衰などはここが基準

            if (_camera == null) return ;

            // 角度更新 カメラ基準
            var forward = _camera.forward.normalized;
            var up = _camera.up.normalized;
            sePlayer.Listener.SetOrientation(forward.x, forward.y, forward.z, up.x, up.y, up.z); // パン(左右バランス)はここが基準
            sePlayer.Listener.Update();
        }

        /// <summary>
        /// サウンドのListenerをオブジェクトにアタッチする
        /// </summary>
        /// <param name="cam"></param>
        public void AttachPlayer(Transform obj) => _follow = obj;

        /// <summary>
        /// 音を視界基準で聞くため、カメラをアタッチする
        /// </summary>
        /// <param name="obj"></param>
        public void AttachCamera(Transform obj) => _camera = obj;

    }
}