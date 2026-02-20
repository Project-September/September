using Fusion;
using September.Common;
using TMPro;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerNameUI : NetworkBehaviour
    {
        [SerializeField] private Canvas _root;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private float _maxDistance = 30f;

        private Camera _camera;
        private PlayerRef _ownerRef;
        private bool _isMine;

        public override void Spawned()
        {
            _camera = Camera.main;
            _ownerRef = Object.InputAuthority;
            _isMine = _ownerRef == Runner.LocalPlayer;

            if (_isMine)
            {
                _root.gameObject.SetActive(false);
                return;
            }

            TrySetName();
        }

        private void TrySetName()
        {
            if (PlayerDatabase.Instance == null) return;

            if (PlayerDatabase.Instance.PlayerDataDic
                .TryGet(_ownerRef, out var data))
            {
                _nameText.text = data.DisplayNickName;
            }
        }

        private void LateUpdate()
        {
            if (_isMine) return;

            if (!_camera)
            {
                _camera = Camera.main;
                if (!_camera) return;
            }

            RotateToCamera();
        }

        private void RotateToCamera()
        {
            // ★ 位置は一切触らない
            _root.transform.rotation = _camera.transform.rotation;
        }
    }
}