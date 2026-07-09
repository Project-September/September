using Fusion;
using InGame.Exhibit;
using September.InGame.Common;
using September.InGame.UI;

namespace September.Common
{
    public class TagRulePlayerUIPresenter : NetworkBehaviour
    {
        private void Start()
        {
            var inGameManager = StaticServiceLocator.Instance.Get<InGameManager>();

            //  ゲーム開始表示終了後に役職開示を行う
            inGameManager.GameStarted += () =>
            {
                ShowOgreLamp();
                ShowStatusUpUI();
            };
        }

        public void OnKilled(PlayerRef killer, PlayerRef victim)
        {
            RPC_ShowKillLog(killer, victim);

            SessionPlayerData killerData = PlayerDatabase.Instance.PlayerDataDic.Get(killer);
            if (killerData.IsOgre && killer != victim)
            {
                OnChangeOgre(killer, victim);
            }
        }

        private void OnChangeOgre(PlayerRef killer, PlayerRef victim)
        {
            RPC_SetOgreUI(killer, victim);
            RPC_ShowStatusUpUI(killer, false);
            RPC_ShowStatusUpUI(victim, true);
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

        // 鬼変更時の鬼ランプの表示・非表示処理
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SetOgreUI(PlayerRef executor, PlayerRef targetRef)
        {
            // 鬼じゃなかったら消す
            if (executor == Runner.LocalPlayer)
            {
                UIController.I.ShowOgreLamp(false);
            }

            // 鬼だったら付ける
            else if (targetRef == Runner.LocalPlayer)
            {
                UIController.I.ShowOgreLamp(true);
                UIController.I.ChangeTagNotice(2);
            }
        }

        private void ShowStatusUpUI()
        {
            if (PlayerDatabase.Instance.PlayerDataDic[Runner.LocalPlayer].IsOgre)
            {
                UIController.I.ShowStatusUpUI(-1, StatusUpType.Ogre);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ShowStatusUpUI(PlayerRef playerRef, bool showStatusUpUI)
        {
            if (Runner.LocalPlayer == playerRef)
            {
                if (showStatusUpUI)
                {
                    UIController.I.ShowStatusUpUI(-1, StatusUpType.Ogre);
                }
                else
                {
                    UIController.I.ShowStatusUpUI(-1, StatusUpType.None);
                }
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ShowKillLog(PlayerRef killer, PlayerRef killed)
        {
            if (PlayerDatabase.Instance.PlayerDataDic.TryGet(killer, out SessionPlayerData killerData) &&
                PlayerDatabase.Instance.PlayerDataDic.TryGet(killed, out SessionPlayerData killedData))
            {
                string killerName = killerData.DisplayNickName;
                string killedName = killedData.DisplayNickName;
                UIController.I.ShowLog($"{killerName} が {killedName} を倒した");
            }
        }
    }
}
