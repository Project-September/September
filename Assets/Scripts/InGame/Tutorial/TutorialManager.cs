using Fusion;
using System.Collections.Generic;
using UnityEngine;
using InGame.Player;
using TMPro;

namespace September.InGame.Tutorial
{
    public class TutorialManager : NetworkBehaviour
    {
        [Header("チュートリアルのアクションリスト")]
        [SerializeReference, SubclassSelector] private List<TutorialActionBase> _tutorialActions;
        [Header("説明画像を表示するImage")]
        [SerializeField] private GameObject _tutorialUI;
        [Header("説明文を表示するText")]
        [SerializeField] private TextMeshProUGUI _tutorialText;
        [Header("アクションを完了する条件表示")]
        [SerializeField] private TextMeshProUGUI _actionConditionText;
        private int _currentActionIndex = 0;

        private bool _isTutorialCompleted = false;
        private TutorialActionData _actionData;

        public override void Spawned()
        {
            base.Spawned();
        }

        private void Awake()
        {
            _actionData = new TutorialActionData
            {
                Action = OnCompleteCurrentAction,
                TutorialUI = _tutorialUI,
                TutorialText = _tutorialText,
                ActionConditionText = _actionConditionText
            };
        }

        /// <summary>
        /// チュートリアルを開始するメソッド
        /// 最初のアクションを開始し、以降は各アクションの完了コールバックで次のアクションを開始する
        /// </summary>
        public void OnTutorialStart(NetworkObject player, PlayerInputManager playerInputManager)
        {
            Debug.Log($"TutorialManager: OnTutorialStart called for player {player.name}");
            _actionData.Player = player.gameObject;
            _actionData.PlayerInputManager = playerInputManager;
            _tutorialActions[_currentActionIndex].OnStart(_actionData);
        }

        private void Update()
        {
            if (_isTutorialCompleted) return;
            _tutorialActions[_currentActionIndex].OnUpdate();
        }

        private void OnCompleteCurrentAction()
        {
            // 現在のアクションを終了する
            _tutorialActions[_currentActionIndex].OnEndAction();
            _currentActionIndex++;
            // 次のアクションがあれば開始する
            if (_currentActionIndex < _tutorialActions.Count)
            {
                _tutorialActions[_currentActionIndex].OnStart(_actionData);
            }
            // すべてのアクションが完了した場合の処理
            else
            {
                Debug.Log("チュートリアルが完了しました！");
                _isTutorialCompleted = true;
            }
        }
    }
}
