using Fusion;
using InGame.Exhibit;
using September.InGame.Common;
using September.InGame.UI;

namespace September.Common
{
    public class TagRuleGameStartedUIPresenter : NetworkBehaviour, IGameStartedPresenter
    {
        private void Start()
        {
            var inGameManager = StaticServiceLocator.Instance.Get<InGameManager>();

            //  ゲーム開始表示終了後に役職開示を行う
            inGameManager.GameStarted += OnGameStarted;
        }

        public void OnGameStarted()
        {
            ShowOgreLamp();
            ShowStatusUpUI();
        }

        // 役職開示時のテキスト表示
        private void ShowOgreLamp()
        {
            // 鬼だったら「あなたが鬼です」と表示
            if (PlayerDatabase.Instance.PlayerDataDic[Runner.LocalPlayer].IsOgre)
            {
                UIController.I.ShowOgreLamp(true);
                UIController.I.ChangeTagNotice(0);
            }
            // 鬼じゃなかったら「鬼に選ばれなかった」と表示
            else
            {
                UIController.I.ChangeTagNotice(1);
            }
        }

        // 鬼状態のバフ効果を表示
        private void ShowStatusUpUI()
        {
            if (PlayerDatabase.Instance.PlayerDataDic[Runner.LocalPlayer].IsOgre)
            {
                UIController.I.ShowStatusUpUI(-1, StatusUpType.Ogre);
            }
        }
    }
}
