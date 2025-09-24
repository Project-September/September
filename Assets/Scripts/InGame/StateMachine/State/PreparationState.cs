using System;
using System.Linq;
using Cinemachine;
using CRISound;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Fusion;
using InGame.Common;
using InGame.Exhibit;
using InGame.Health;
using InGame.Player;
using September.InGame.Common;
using September.InGame.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace September.Common
{
    public class PreparationState : ImtStateMachine<InGameManager>.State
    {
        [SerializeField] private Transform[] _spawnPositions;
        [SerializeField] private Image _fadeImage;
        [SerializeField] private CinemachineVirtualCamera _startCamera;
        [SerializeField] private Vector3 _cameraOffset;
        [SerializeField] private SetIcon _setIcon;
        private int _spawnPositionIndex; 
        private PlayerRef _firstOgrePlayer;
        private static readonly string _cueSheetName = "ALLCue";
        protected internal override void OnEnter()
        {
            if (_fadeImage) _fadeImage.gameObject.SetActive(true);
            HideCursor();
            UIController.I.SetUpStartUI();
            
            if (PlayerDatabase.Instance.PlayerDataDic.TryGet(Context.Runner.LocalPlayer, out SessionPlayerData localData))
            {
                if (localData.CharacterType == CharacterType.Sarutobi) 
                {
                    UIController.I.ChangeDescriptionUI(3);
                }
            }
            
            if (Context.Runner.IsServer)
            {
                ChooseOgre();
                Initialize().Forget();
            }
        }

        private async UniTask Initialize()
        {
            await Runner.LoadScene("Field", LoadSceneMode.Additive);
            var container = CharacterDataContainer.Instance;
            foreach (var pair in PlayerDatabase.Instance.PlayerDataDic)
            {
                var player = await Context.Runner.SpawnAsync(
                    container.GetCharacterData(pair.Value.CharacterType).Prefab,
                    GetSpawnPosition(),
                    inputAuthority: pair.Key);
                Context.Runner.SetPlayerObject(pair.Key, player);
                if (!Context.PlayerDataDic.ContainsKey(pair.Key))
                {
                    Context.AddPlayerObject(pair.Key, player);
                }
                var playerHealth = player.GetComponent<PlayerHealth>();
                //PlayerHealthのOnDeathに登録
                playerHealth.OnDeath += OnPlayerKilled;
                var spd = pair.Value;
                foreach (var data in PlayerDatabase.Instance.PlayerDataDic)
                {
                    if(pair.Key == data.Key) continue;
                    spd.StunData.Add(data.Key, 0);
                }
                PlayerDatabase.Instance.PlayerDataDic.Set(pair.Key,spd);
                _setIcon.ShowIcon(pair.Key);
            }
            
            Context.Register(StaticServiceLocator.Instance);
            RPC_OpeningSequence();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_OpeningSequence()
        {
            OpeningSequence().Forget();
        }

        private async UniTaskVoid OpeningSequence()
        {
            GameInput.I.ToggleMoveInput(false);
            GameInput.I.ToggleActionInput(false);
            _startCamera.Priority = 999;
            _startCamera.ForceCameraPosition(_spawnPositions[0].position + _cameraOffset, Quaternion.identity);
            //  黒画面フェードアウト
            await FadeOut();
            //  各プレイヤーに注目 + エモート
            await StartAnimation();
            _startCamera.Priority = -999;
            //  カメラが元の位置に戻るまで待つ
            await UniTask.WaitForSeconds(1.5f);
            //  カウントダウン開始 
            UIController.I.TimeOverlayMessage?.Invoke(TimeMessageType.Countdown);
            await UniTask.WaitForSeconds(3f);
            //  準備フェーズ
            GameInput.I.ToggleMoveInput(true);
            BGMManager.ReleseFlag();
            UIController.I.StartTimer(Context.Runner);
            await UniTask.Delay(TimeSpan.FromSeconds(10f));
            //  ゲーム開始
            GameInput.I.ToggleActionInput(true);
            var task = UIController.I.TimeOverlayMessage?.Invoke(TimeMessageType.GameStart);
            //  ゲーム開始表示が正常に行われたら表示終了後に役職開示を行う
            if (task != null)
                task.Value.GetAwaiter().OnCompleted(SetOgreLamp);
            else
                SetOgreLamp();
            await UniTask.WaitForSeconds(3f);
            ShowStatusUpUI();
            _firstOgrePlayer = PlayerRef.None;
            if (Context.Runner.IsServer)
            {
                //  ステート終了
                Context.Rpc_SendEvent((int)StateEventId.Finish);
            }
        }
        
        // 全ての準備が整ったらFadeをあける
        private async UniTask FadeOut()
        {
            if (_fadeImage)
            {
                _fadeImage.color = new Color(0f, 0f, 0f, 1f);

                await _fadeImage.DOFade(0f, 1f).SetEase(Ease.InOutQuad);
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1f));
            }
        }

        // ゲームスタート前にPlayerがポーズする
        private async UniTask StartAnimation()
        {
            var playerDatabase = PlayerDatabase.Instance;
            var characterDataContainer = CharacterDataContainer.Instance;
            int index = 0;
            foreach (var pair in playerDatabase.PlayerDataDic)
            {
                var animClipPlayer = Context.Runner.GetPlayerObject(pair.Key).GetComponent<AnimationClipPlayer>();
                var characterType = pair.Value.CharacterType;
                var emoteClip = characterDataContainer.GetCharacterData(characterType).EmoteAnimation;
                _startCamera.transform.position = _spawnPositions[index].position + _cameraOffset;
                await UniTask.WaitForSeconds(1f);
                float delayTime = 1f;
                if (emoteClip)
                {
                    if(Context.Runner.IsServer) animClipPlayer.PlayClip(emoteClip);
                    var cueName = characterDataContainer.GetCharacterData(characterType).StartVoice;
                    CRIAudio.PlaySE(_cueSheetName,cueName); // ボイス呼び出し
                    delayTime = emoteClip.length;
                }
                await UniTask.WaitForSeconds(delayTime); // 各エモートのAnimation分待つ
                index++;
            }
        }
        
        private void UpdateStunData(PlayerRef killerRef, SessionPlayerData killerData, PlayerRef killedPlayer)
        {
            if (killerData.StunData.TryGet(killedPlayer, out int count))
            {
                killerData.StunData.Set(killedPlayer, count + 1);
            }
            else
            {
                killerData.StunData.Set(killedPlayer, 1);
            }
            PlayerDatabase.Instance.PlayerDataDic.Set(killerRef, killerData);
            
            // スコアの更新処理
            PlayerDatabase.Instance.Server_RecalculateScore(killerRef);
        }
        private Vector3 GetSpawnPosition()
        {
            var result = _spawnPositions[_spawnPositionIndex].position;
            _spawnPositionIndex = (_spawnPositionIndex + 1) % _spawnPositions.Length;
            return result;
        }
        /// <summary>
        /// 鬼を抽選するメソッド
        /// </summary>
        private void ChooseOgre()
        {
            var dic = PlayerDatabase.Instance.PlayerDataDic;
            if (dic.Count <= 0 || !Context.Runner.IsServer) 
                return;
            
            var index = Random.Range(0, dic.Count);
            var ogreKey = dic.ToArray()[index].Key;
            var data = dic.Get(ogreKey);
            data.IsOgre = true;
            PlayerDatabase.Instance.PlayerDataDic.Set(ogreKey, data);
            _firstOgrePlayer = ogreKey;
        }
        /// <summary>
        /// 各Playerの気絶時に呼ばれるメソッド
        /// </summary>
        private void OnPlayerKilled(HitData data)
        {
            if (!Context.Runner.IsServer) return; 
            //.Instance.Server_AddStun(data.ExecutorRef);
            
            SessionPlayerData killerData = PlayerDatabase.Instance.PlayerDataDic.Get(data.ExecutorRef);
            var killedData = PlayerDatabase.Instance.PlayerDataDic.Get(data.TargetRef);
            if (killerData.IsOgre && data.ExecutorRef != data.TargetRef)
            {
                killerData.IsOgre = false;
                RPC_ShowStatusUpUI(data.ExecutorRef, false);
                PlayerDatabase.Instance.PlayerDataDic.Set(data.ExecutorRef, killerData);
                killedData.IsOgre = true;
                PlayerDatabase.Instance.PlayerDataDic.Set(data.TargetRef, killedData);
                RPC_ShowStatusUpUI(data.TargetRef, true);
                RPC_SetOgreUI(data.ExecutorRef,data.TargetRef);
            }
            if(data.ExecutorRef == data.TargetRef) 
                return;
            
            UpdateStunData(data.ExecutorRef, killerData, data.TargetRef);
            Rpc_ShowKillLog(data.ExecutorRef, data.TargetRef);
        }
        private void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        private void SetOgreLamp()
        {
            if (PlayerDatabase.Instance.PlayerDataDic[Context.Runner.LocalPlayer].IsOgre)
            {
                UIController.I.ShowOgreLamp(true);
                UIController.I.ChangeTagNotice(0);
            }
            else
            {
                UIController.I.ChangeTagNotice(1);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_ShowKillLog(PlayerRef killer, PlayerRef killed)
        {
            if (PlayerDatabase.Instance.PlayerDataDic.TryGet(killer, out SessionPlayerData killerData) &&
                PlayerDatabase.Instance.PlayerDataDic.TryGet(killed, out SessionPlayerData killedData))
            {
                string killerName = killerData.DisplayNickName;
                string killedName = killedData.DisplayNickName;
                UIController.I.ShowLog($"{killerName} が {killedName} を倒した");
            }
        }
        
        // 鬼変更時のUI更新通知
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SetOgreUI(PlayerRef executor, PlayerRef targetRef)
        {
            if (executor == Context.Runner.LocalPlayer)
            {
                UIController.I.ShowOgreLamp(false);
            }
            else if (targetRef == Context.Runner.LocalPlayer)
            {
                UIController.I.ShowOgreLamp(true);
                UIController.I.ChangeTagNotice(2);
            }
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ShowStatusUpUI(PlayerRef playerRef,bool showStatusUpUI)
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

        private void ShowStatusUpUI()
        {
            if (PlayerDatabase.Instance.PlayerDataDic[Runner.LocalPlayer].IsOgre)
            {
                UIController.I.ShowStatusUpUI(-1, StatusUpType.Ogre);
            }
        }
       
    }
}