using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace September.InGame.Tutorial
{
    public class TutorialManager : NetworkBehaviour
    {
        /// <summary>チュートリアルでプレイさせたいアクション配列</summary>
        [SerializeReference, SubclassSelector] private List<TutorialActionBase> _tutorialActions;
        private int _currentActionIndex = 0;

        private bool _isTutorialCompleted = false;

        public override void Spawned()
        {
            base.Spawned();
        }

        private void Start()
        {
            // 仮のチュートリアル開始(仕様が決まり次第、適切なタイミングで呼び出す)
            OnTutorialStart();
        }

        /// <summary>
        /// チュートリアルを開始するメソッド
        /// 最初のアクションを開始し、以降は各アクションの完了コールバックで次のアクションを開始する
        /// </summary>
        public void OnTutorialStart()
        {
            _tutorialActions[_currentActionIndex].OnStart(OnCompleteCurrentAction);
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
                _tutorialActions[_currentActionIndex].OnStart(OnCompleteCurrentAction);
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
