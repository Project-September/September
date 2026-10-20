using Fusion;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace InGame.Player.Okubo
{
    // 本体のアニメーションDirectorに、ローカルのカメラ演出だけを同期する。
    public sealed class OkuboCutInCamera : MonoBehaviour
    {
        [SerializeField] private PlayableDirector _animationDirector;
        [SerializeField] private PlayableDirector _cameraDirector;
        [SerializeField] private GameObject _cameraRoot;
        private NetworkObject _owner;

        private void Awake()
        {
            _owner = GetComponentInParent<NetworkObject>();
            _cameraRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (!_animationDirector) return;
            _animationDirector.played += OnPlayed;
            _animationDirector.stopped += OnStopped;
        }

        private void OnDisable()
        {
            if (_animationDirector)
            {
                _animationDirector.played -= OnPlayed;
                _animationDirector.stopped -= OnStopped;
            }
            OnStopped(_animationDirector);
        }

        private void OnPlayed(PlayableDirector director)
        {
            if (!_owner || !_owner.HasInputAuthority || !_cameraDirector || !_cameraRoot) return;
            var mainCamera = Camera.main;
            var brain = mainCamera ? mainCamera.GetComponent<CinemachineBrain>() : null;
            if (!brain)
            {
                Debug.LogWarning("Okuboカットイン: MainCameraのCinemachineBrainが見つかりません。", this);
                return;
            }

            // 既存Timelineのカメラ出力との競合を避ける。本体のモーション出力は維持する。
            foreach (var binding in director.playableAsset.outputs)
                if (binding.outputTargetType == typeof(CinemachineBrain))
                    director.SetGenericBinding(binding.sourceObject, null);
            director.RebindPlayableGraphOutputs();

            foreach (var binding in _cameraDirector.playableAsset.outputs)
                if (binding.outputTargetType == typeof(CinemachineBrain))
                    _cameraDirector.SetGenericBinding(binding.sourceObject, brain);

            _cameraRoot.SetActive(true);
            _cameraDirector.time = director.time;
            _cameraDirector.Play();
            _cameraDirector.Evaluate();
        }

        private void OnStopped(PlayableDirector director)
        {
            if (_cameraDirector) _cameraDirector.Stop();
            if (_cameraRoot) _cameraRoot.SetActive(false);
        }
    }
}
