using Cysharp.Threading.Tasks;
using DG.Tweening;
using September.Common;
using September.InGame.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace September
{
    public class TutorialEndButton : MonoBehaviour
    {
        [SerializeField] private Button _tutorialButton;
        [SerializeField] private Image _fadeImage;
        private Tween _fadeTween;

        private void Start()
        {
            if (_tutorialButton) _tutorialButton.onClick.AddListener(() => ExitToTitleAsync().Forget());
        }

        private async UniTaskVoid ExitToTitleAsync()
        {
            var token = this.GetCancellationTokenOnDestroy();
            if (_fadeImage)
            {
                _fadeImage.color = Color.black;
                _fadeTween = _fadeImage.DOFade(1f, 1f).SetEase(Ease.InOutQuad);
                await _fadeTween.ToUniTask(cancellationToken: token);
                _fadeImage.raycastTarget = false;
            }
            ExitTutorial();
        }

        private void ExitTutorial()
        {
            // 単体起動とTitle経由の終了を、開始時のRunnerを管理するSetupに任せる。
            var setup = FindFirstObjectByType<TutorialSceneSetup>();
            if (setup) setup.ExitToTitleAsync().Forget();
            else if (NetworkManager.Instance) NetworkManager.Instance.QuitLobby().Forget();
        }

        private void OnDestroy()
        {
            if (_tutorialButton) _tutorialButton.onClick.RemoveListener(ExitTutorial);
        }
    }
}
