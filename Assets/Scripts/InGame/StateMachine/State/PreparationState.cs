using System;
using System.Linq;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Fusion;
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
        private int _spawnPositionIndex; 
        [SerializeField] private Vector3 _cameraOffset;
        [SerializeField] private int _emoteDelay;
        protected internal override void OnEnter()
        {
            if (_fadeImage) _fadeImage.gameObject.SetActive(true);
            HideCursor();
            SetUpUI();
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
                if (playerHealth) playerHealth.OnDeath += OnPlayerKilled;
                //PlayerHealthのOnDeathに登録
            }
            Context.Register(StaticServiceLocator.Instance);
            RPC_SetCameraPriority(20);
            // ToDo : ここにAnimation処理
            // ToDo : Animationが終了するまで入力を受け付けなくする
            RPC_FadeAndAnimation();
            StartTimer().Forget();
        }

        [Rpc]
        private void RPC_FadeAndAnimation()
        {
            FadeAndAnimation().Forget();
        }

        private async UniTaskVoid FadeAndAnimation()
        {
            await FadeIn();
            await StartAnimation();
        }
        
        // 全ての準備が整ったらFadeをあける
        private async UniTask FadeIn()
        {
            if (_fadeImage)
            {
                _fadeImage.color = new Color(1f, 1f, 1f, 1f);

                await _fadeImage.DOFade(0f, 1f).SetEase(Ease.InOutQuad);
                Debug.Log("Fadeの終了");
            }
        }

        // ゲームスタート前にPlayerがポーズする
        private async UniTask StartAnimation()
        {
            _startCamera.gameObject.transform.position =  _spawnPositions[0].position + _cameraOffset;
            for (int i = 0; i < _spawnPositionIndex; i++)
            {
                var position = _spawnPositions[i].position + _cameraOffset;
                _startCamera.gameObject.transform.Translate(position);
                await UniTask.Delay(TimeSpan.FromSeconds(_emoteDelay)); // 各エモートのAnimation分待つ
            }
            // 仮実装
            // Debug.Log("Animation Start");
            // await UniTask.Delay(5000);
            // Debug.Log("Animation End");
            RPC_SetCameraPriority(0);
        }
        
        private Vector3 GetSpawnPosition()
        {
            var result = _spawnPositions[_spawnPositionIndex].position;
            _spawnPositionIndex = (_spawnPositionIndex + 1) % _spawnPositions.Length;
            return result;
        }
        private void SetUpUI()
        {
            UIController.I.SetUpStartUI();
            UIController.I.StartTimer();
        }
        /// <summary>
        /// 鬼を抽選するメソッド
        /// </summary>
        private void ChooseOgre()
        {
            var dic = PlayerDatabase.Instance.PlayerDataDic;
            if (dic.Count <= 0 || !Context.Runner.IsServer) return;
            
            var index = Random.Range(0, dic.Count);
            var ogreKey = dic.ToArray()[index].Key;
            var data = dic.Get(ogreKey);
            data.IsOgre = true;
            PlayerDatabase.Instance.PlayerDataDic.Set(ogreKey, data);
            RPC_SetOgreLamp(ogreKey);
        }
        /// <summary>
        /// 各Playerの気絶時に呼ばれるメソッド
        /// </summary>
        private void OnPlayerKilled(HitData data)
        {
            if (!Context.Runner.IsServer) return; // サーバー側でのみ実行可能
            
            var killerData = PlayerDatabase.Instance.PlayerDataDic.Get(data.ExecutorRef); //DataBaseから該当Playerの情報取得
            killerData.IsOgre = false;
            PlayerDatabase.Instance.PlayerDataDic.Set(data.ExecutorRef, killerData); //DataBase更新 

            var killedData = PlayerDatabase.Instance.PlayerDataDic.Get(data.TargetRef);
            killedData.IsOgre = true;
            PlayerDatabase.Instance.PlayerDataDic.Set(data.TargetRef, killedData);
            killerData.Score += Context.AddScore;
            RPC_SetOgreUI(data.ExecutorRef,data.TargetRef);
        }
        private async UniTask StartTimer()
        {
            for (int i = Context.TimerData.PreStartTime; i >= 1; i--)
            {
                //ReadyTime表示
                await UniTask.Delay(TimeSpan.FromSeconds(Context.TimerData.Duration), cancellationToken: Context.Cts.Token);
            }
            await UniTask.Delay(TimeSpan.FromSeconds(Context.TimerData.AfterReadyDelay), cancellationToken: Context.Cts.Token);
            //  ステート終了
            Context.Rpc_SendEvent((int)StateEventId.Finish);
        }
        private void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        // 鬼変更時のUI更新通知
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SetOgreUI(PlayerRef executor, PlayerRef targetRef)
        {
            UIController.I.ShowNoticeKillLog($"鬼が{executor}から{targetRef}に変更された");
            
            if (executor == Context.Runner.LocalPlayer)
                UIController.I.ShowOgreLamp(false);
            else if(targetRef == Context.Runner.LocalPlayer)
                UIController.I.ShowOgreLamp(true);
        }
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SetOgreLamp(PlayerRef ogreRef)
        {
            if (ogreRef == Context.Runner.LocalPlayer)
            {
                UIController.I.ShowOgreLamp(true);
            }
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SetCameraPriority(int priority)
        {
            _startCamera.Priority = priority;
        }
    }
}