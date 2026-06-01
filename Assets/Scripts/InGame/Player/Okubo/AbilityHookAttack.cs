using System.Collections.Generic;
using Fusion;
using September.Common;
using UnityEngine;

namespace InGame.Player.Okubo
{
    public class AbilityHookAttack : NetworkBehaviour
    {
        [SerializeField] private PlayerInputManager _playerInputManager;
        [SerializeField] private Material _wireMaterial;
        [SerializeField] private Transform _hookOrigin;
        [SerializeField] private float _stretchSpeed;
        [SerializeField] private float _pullSpeed;
        [SerializeField] private float _wireLength;
        [SerializeField] private float _stretchedWaitTime = 0.3f;
        [SerializeField] private float _coolDownTime = 1.0f;
        [SerializeField] private float _wireThickness;


        private Transform _wireCyl;
        private HookAttackState _currentState;
        private float _currentHookLength;
        private float _waitTimer;
        private List<GameObject> _players;

        public override void Spawned()
        {
            var wireObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wireObj.name = "HookWireCylinder";
            _wireCyl = wireObj.transform;
            _wireCyl.localScale = Vector3.one * _wireThickness;
            wireObj.GetComponent<Renderer>().sharedMaterial = _wireMaterial;
            if (wireObj.TryGetComponent(out Collider col)) Destroy(col);
            _wireCyl.gameObject.SetActive(false);
        }

        public override void FixedUpdateNetwork()
        {
            _playerInputManager.GetPlayerInput(out var input);

            if (!HasInputAuthority) return;

            switch (_currentState)
            {
                case HookAttackState.Idol:
                    //フック攻撃開始
                    if (input.Buttons.IsSet(PlayerButtons.Ability2))
                        ChangeState(HookAttackState.Stretching);
                    break;
                case HookAttackState.Stretching:
                    OnStretching();
                    break;
                case HookAttackState.Pulling:
                    OnPulling();
                    break;
                case HookAttackState.Stretched:
                case HookAttackState.CoolDown:
                    //待機処理
                    OnWait();
                    break;
            }

            Debug.Log("Hook Length" + _currentHookLength);
        }
        private void ChangeState(HookAttackState state)
        {
            _currentState = state;

            switch (state)
            {
                case HookAttackState.Stretching:
                    _wireCyl.gameObject.SetActive(true);
                    break;
                case HookAttackState.Stretched:
                    _waitTimer = _stretchedWaitTime;
                    break;

                case HookAttackState.CoolDown:
                    _wireCyl.gameObject.SetActive(false);
                    _waitTimer = _coolDownTime;
                    break;
            }
        }

        private void OnStretching()
        {
            _currentHookLength += _wireLength / _stretchSpeed * Runner.DeltaTime;

            //最大の長さまで伸びた
            if (_currentHookLength >= _wireLength)
            {
                _currentHookLength = _wireLength;
                ChangeState(HookAttackState.Stretched);
            }

            UpdateHookLength(_currentHookLength, this.transform.forward);
        }

        private void OnPulling()
        {
            _currentHookLength -= _wireLength / _pullSpeed * Runner.DeltaTime;

            if (_currentHookLength <= 0)
            {
                _currentHookLength = 0;
                ChangeState(HookAttackState.CoolDown);
            }

            UpdateHookLength(_currentHookLength, transform.forward);
        }

        private void OnWait()
        {
            _waitTimer -= Runner.DeltaTime;

            if (_waitTimer > 0)
                return;

            switch (_currentState)
            {
                case HookAttackState.Stretched:
                    ChangeState(HookAttackState.Pulling);
                    break;

                case HookAttackState.CoolDown:
                    ChangeState(HookAttackState.Idol);
                    break;
            }
        }

        private void UpdateHookLength(float length, Vector3 direction)
        {
            direction = direction.normalized;

            // 長さ変更
            Vector3 scale = _wireCyl.localScale;
            scale.y = length * 0.5f; // Cylinderは高さ2が基準
            _wireCyl.localScale = scale;

            // 中心位置を始点から length/2 の位置へ
            _wireCyl.position = _hookOrigin.transform.position + direction * (length * 0.5f);

            // 向きを合わせる
            _wireCyl.up = direction;
        }
        public enum HookAttackState
        {
            Idol, Stretching, Stretched, Pulling, CoolDown
        }
    }
}
