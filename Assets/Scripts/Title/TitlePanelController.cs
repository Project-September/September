// TitlePanelController.cs  (Esc と Idle を内包した統合版)
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace September.Title
{
    public class TitlePanelController : MonoBehaviour
    {
        public enum PanelType
        {
            MainMenu,
            HostRoom,
            JoinRoom,
            Stats,
            Options,
            Credits,
            UserProfile,
            Idle,
            HowToPlay
        }

        [Header("Panels (PanelController付き)")]
        [SerializeField] private PanelController _mainMenuPanel;
        [SerializeField] private PanelController _hostRoomPanel;
        [SerializeField] private PanelController _joinRoomPanel;
        [SerializeField] private PanelController _statsPanel;
        [SerializeField] private PanelController _optionsPanel;
        [SerializeField] private PanelController _creditsPanel;
        [SerializeField] private PanelController _userProfilePanel;
        [SerializeField] private PanelController _idlePanel;
        [SerializeField] private PanelController _howToPlayPanel;
        

        private Dictionary<PanelType, PanelController> _map;

        [Header("Start Settings")]
        [SerializeField] private PanelType _startPanel = PanelType.MainMenu;

        [Header("Debug / State (実行中に現在の状態を表示)")]
        [field: SerializeField] public PanelType CurrentPanel { get; private set; }

        [Header("Idle Settings")]
        [SerializeField] private float _idleSeconds = 30f;

        private float _lastInputTime;

        private void Awake()
        {
            _map = new Dictionary<PanelType, PanelController>
            {
                { PanelType.MainMenu, _mainMenuPanel },
                { PanelType.HostRoom, _hostRoomPanel },
                { PanelType.JoinRoom, _joinRoomPanel },
                { PanelType.Stats, _statsPanel },
                { PanelType.Options, _optionsPanel },
                { PanelType.Credits, _creditsPanel },
                { PanelType.UserProfile, _userProfilePanel },
                { PanelType.Idle, _idlePanel },
                { PanelType.HowToPlay, _howToPlayPanel}
            };
        }

        private void Start()
        {
            HideAllPanels();
            CurrentPanel = _startPanel;
            ShowPanel(_startPanel);
            
            ResetIdleTimer();
            RunIdleLoop().Forget();
        }

        private void Update()
        {
            if (Keyboard.current.anyKey.wasPressedThisFrame ||
                Mouse.current.delta.ReadValue() != Vector2.zero ||
                (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame))
            {
                ResetIdleTimer();
            }
        }

        private async UniTaskVoid RunIdleLoop()
        {
            var token = this.GetCancellationTokenOnDestroy();
            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: token);

                if (Time.time - _lastInputTime > _idleSeconds)
                {
                    if (_idlePanel != null && !_idlePanel.gameObject.activeSelf)
                    {
                        ShowPanel(PanelType.Idle);
                    }
                }
            }
        }

        private void ResetIdleTimer()
        {
            _lastInputTime = Time.time;

            if (_idlePanel != null && _idlePanel.gameObject.activeSelf && CurrentPanel != PanelType.Idle)
            {
                _idlePanel.HidePanel();
            }
        }

        private void HideAllPanels()
        {
            foreach (var kv in _map.Where(kv => kv.Value != null))
            {
                kv.Value.HidePanel();
            }
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }

        public void ShowPanel(PanelType type)
        {
            HideAllPanels();

            if (_map.TryGetValue(type, out var target) && target != null)
            {
                target.ShowPanel();
                CurrentPanel = type;
            }

            ResetIdleTimer();
        }

        // UnityEvent から直接呼べるショートカット
        public void ShowMainMenu()    => ShowPanel(PanelType.MainMenu);
        public void ShowHostRoom()    => ShowPanel(PanelType.HostRoom);
        public void ShowJoinRoom()    => ShowPanel(PanelType.JoinRoom);
        public void ShowStats()       => ShowPanel(PanelType.Stats);
        public void ShowOptions()     => ShowPanel(PanelType.Options);
        public void ShowCredits()     => ShowPanel(PanelType.Credits);
        public void ShowUserProfile() => ShowPanel(PanelType.UserProfile);
        public void ShowIdle()        => ShowPanel(PanelType.Idle);
        public void ShowHowToPlay()   => ShowPanel(PanelType.HowToPlay);

        // Esc/キャンセルでメインに戻す
        public void BackToMain()
        {
            // Idle からでも必ず MainMenu へ
            ShowMainMenu();
        }
    }
}
