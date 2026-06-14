using System;
using System.Linq;
using Cinemachine;
using CRISound;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Fusion;
using GameEvent;
using InGame.Common;
using InGame.Exhibit;
using InGame.Health;
using InGame.Player;
using September.InGame.Common;
using September.InGame.Common.Stats;
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
        [SerializeField, Tooltip("開始時のPlayerの位置をランダム化する")] private bool _isRandomSpawn = false;
        private PlayerRef _firstOgrePlayer;
        private bool _hasRecordedPlayerSelection = false;
        private static readonly string _cueSheetName = "ALLCue";
        public bool IsRandomSpawn { get => _isRandomSpawn; set => _isRandomSpawn = value; }
        protected internal override void OnEnter()
        {
            if (_fadeImage) _fadeImage.gameObject.SetActive(true);
            HideCursor();
            UIController.I.SetUpStartUI();

            if (PlayerDatabase.Instance.PlayerDataDic.TryGet(Context.Runner.LocalPlayer, out SessionPlayerData localData))
            {
                ControlDescriptionType type = CharacterDataContainer.Instance.GetControlDescriptionType(localData.CharacterType);

                UIController.I.ChangeDescriptionUI(type);
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
            await SpawnPlayers();

            // プレイヤーのキャラクター選択を一度だけ記録
            if (!_hasRecordedPlayerSelection)
            {
                _hasRecordedPlayerSelection = true;
                foreach (var pair in PlayerDatabase.Instance.PlayerDataDic)
                {
                    var data = new GameEventData();
                    data.DataPack("Player", pair.Key.ToString());
                    data.DataPack("Chara", pair.Value.CharacterType.ToString());
                    data.DataPack("Name", pair.Value.DisplayNickName);
                    GameEventRecorder.SendEventData("UserSelect", data);
                }
            }

            Context.Register(StaticServiceLocator.Instance);
            RPC_OpeningSequence();
        }

        private async UniTask SpawnPlayers()
        {
            var container = CharacterDataContainer.Instance;
            // キャラクターのスポーン位置をランダムに。
            var spawnPositions = _isRandomSpawn ? GetShuffledSpawnPoints() : _spawnPositions;
            // スポーンポイント設定が適用されていない場合はこのコンポーネント位置を暫定で初期値とする。
            if (spawnPositions == null || spawnPositions.Length == 0)
                spawnPositions = new[] { transform };
            var index = 0;
            foreach (var pair in PlayerDatabase.Instance.PlayerDataDic)
            {
                // ランダム化の際にPlayerのrotationを制御可能にするために、spawnPositionのrotationに合わせる。
                var spawnTransform = spawnPositions[index % spawnPositions.Length];
                var characterData = container.GetCharacterData(pair.Value.CharacterType);
                var prefab = pair.Key.AsIndex >= PlayerDatabase.BotStartIndex ? characterData.BotPrefab : characterData.Prefab;

                var player = await Context.Runner.SpawnAsync(
                    prefab,
                    spawnTransform.position,
                    spawnTransform.rotation,
                    inputAuthority: pair.Key);

                #region ビルドシステム
                var buildGenerator = player.GetComponentInChildren<BuildGenerator>();
                if (buildGenerator != null) buildGenerator.GenerateBuild(pair.Value.BuildType);
#if UNITY_EDITOR
                var root = transform.root;
                if (root != null)
                    Debug.Log($"{root.name} : ビルドシステムの構築に" + (buildGenerator != null ? $"成功しました\n選択ビルド : {pair.Value.BuildType}" : "失敗しました"));
#endif
                #endregion

                PlayerDatabase.Instance.AddPlayerObject(pair.Key, player);

                Context.Runner.SetPlayerObject(pair.Key, player);

                if (!Context.PlayerDataDic.ContainsKey(pair.Key))
                {
                    Context.AddPlayerObject(pair.Key, player);
                }
                var playerHealth = player.GetComponent<PlayerHealth>();
                //PlayerHealthのOnDeathに登録
                playerHealth.OnDeath += OnPlayerKilled;
                var spd = pair.Value;
                foreach (var playerData in PlayerDatabase.Instance.PlayerDataDic)
                {
                    if (pair.Key == playerData.Key) continue;
                    spd.StunData.Add(playerData.Key, 0);
                }
                PlayerDatabase.Instance.PlayerDataDic.Set(pair.Key, spd);
                _setIcon.ShowIcon(pair.Key);

                index++;
            }
        }

        private Transform[] GetShuffledSpawnPoints()
        {
            return _spawnPositions.OrderBy(_ => Random.value).ToArray();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_OpeningSequence()
        {
            OpeningSequence().Forget();
        }

        private async UniTaskVoid OpeningSequence()
        {
            // 全クライアントで入力を無効化
            if (HasStateAuthority) RPC_ToggleInputs(false, false, false);
            _startCamera.Priority = 999;
            _startCamera.ForceCameraPosition(_spawnPositions[0].position + _cameraOffset, Quaternion.identity);
            //  黒画面フェードアウト
            await FadeOut();
            //  各プレイヤーに注目 + エモート
            await StartAnimation();
            _startCamera.Priority = -999;
            // カメラが変わったタイミングで視点入力だけ有効化
            if (HasStateAuthority) RPC_ToggleInputs(false, false, true);
            //  カメラが元の位置に戻るまで待つ
            await UniTask.WaitForSeconds(1.5f);
            //  カウントダウン開始
            if (UIController.I.TimeOverlayMessage != null)
            {
                await UIController.I.TimeOverlayMessage.Invoke(TimeMessageType.Countdown);
            }
            //  準備フェーズ - 全クライアントで移動入力を有効化
            BGMManager.ReleseFlag();

            // タイマー開始
            UIController.I.StartTimer(Context.Runner);

            var countDownDuration = StaticServiceLocator.Instance.Get<InGameManager>().TimerData.PreStartTime;
            if (countDownDuration > 0)// 準備フェーズの時間が0秒以下の場合は下記の処理をスキップする。
            {
                if (HasStateAuthority) RPC_ToggleInputs(true, false, true);

                // 準備フェーズを開始する
                UIController.I.TimeOverlayMessage?.Invoke(TimeMessageType.PreparationStart).Forget();
                // 準備フェーズが終了するまで待機
                await UniTask.Delay(TimeSpan.FromSeconds(countDownDuration));
            }

            //  ゲーム開始 - 全クライアントでアクション入力も有効化
            if (HasStateAuthority) RPC_ToggleInputs(true, true, true);
            //  ゲーム開始表示
            if (UIController.I.TimeOverlayMessage != null)
            {
                await UIController.I.TimeOverlayMessage.Invoke(TimeMessageType.GameStart);
            }

            //  ゲーム開始表示終了後に役職開示を行う
            SetOgreLamp();

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
            foreach (var pair in playerDatabase.PlayerObjectDic)
            {
                var player = pair.Value;
                var animClipPlayer = player.GetComponent<AnimationClipPlayer>();
                var characterType = playerDatabase.PlayerDataDic[pair.Key].CharacterType;
                var emoteClip = characterDataContainer.GetCharacterData(characterType).EmoteAnimation;
                _startCamera.transform.position = player.transform.position + player.transform.rotation * _cameraOffset;
                _startCamera.transform.rotation = player.transform.rotation;
                await UniTask.WaitForSeconds(1f);
                float delayTime = 1f;
                if (emoteClip)
                {
                    if (Context.Runner.IsServer) animClipPlayer.PlayClip(emoteClip);
                    var cueName = characterDataContainer.GetCharacterData(characterType).StartVoice;
                    CRIAudio.PlaySE(_cueSheetName, cueName); // ボイス呼び出し
                    delayTime = emoteClip.length;
                }
                await UniTask.WaitForSeconds(delayTime); // 各エモートのAnimation分待つ
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
            PlayerDatabase.Instance.Server_AddOgreCount(ogreKey);
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
                RPC_SetOgreUI(data.ExecutorRef, data.TargetRef);
                PlayerDatabase.Instance.Server_AddOgreCount(data.TargetRef);
            }
            if (data.ExecutorRef == data.TargetRef)
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

        private void ShowStatusUpUI()
        {
            if (PlayerDatabase.Instance.PlayerDataDic[Runner.LocalPlayer].IsOgre)
            {
                UIController.I.ShowStatusUpUI(-1, StatusUpType.Ogre);
            }
        }

        /// <summary>
        /// 全クライアントの入力状態を切り替えるRPC
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ToggleInputs(bool moveInputEnabled, bool actionInputEnabled, bool lookInputEnabled)
        {
            GameInput.I.ToggleMoveInput(moveInputEnabled);
            GameInput.I.ToggleActionInput(actionInputEnabled);
            GameInput.I.ToggleLookInput(lookInputEnabled);
        }
    }
}