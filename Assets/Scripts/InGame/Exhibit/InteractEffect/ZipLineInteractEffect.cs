using Fusion;
using InGame.Interact;
using September.Common;
using System;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

namespace September
{
    [Serializable]
    public class ZipLineInteractEffect : CharacterInteractEffectBase
    {
        public SplineContainer Spline;
        public GameObject Trolley;
        public float Duration = 5f;
        public float ReturnDuration = 5f;
        [Header("横軸:経過時間の割合(0〜1) 縦軸:スプライン上の位置の割合(0〜1)" +
            "\n始点(t=0)は必ず0、終点(t=1)は必ず1に設定してください")]
        
        public AnimationCurve SpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Trolleyからプレイヤーへの相対オフセット(ぶら下がる位置調整用)")]
        public Vector3 PlayerOffset = new Vector3(0f, -1.5f, 0f);
        private InteractableBase _activeEffect;

        private enum State
        {
            Idle,
            Moving,
            Returning
        }

        private State _currentState = State.Idle;

        private float _timer;
        private NetworkObject _targetPlayerObject;

        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            _activeEffect = target;
            _activeEffect.ForceSetInteractable = false;
            GameInput.I.ToggleMoveInput(false);
            GameInput.I.ToggleActionInput(false);

            PlayerRef playerRef = PlayerRef.FromEncoded(context.Interactor);
            if (PlayerDatabase.Instance.PlayerObjectDic.TryGet(playerRef, out var playerNetworkObject))
            {
                _targetPlayerObject = playerNetworkObject;
                _targetPlayerObject.GetComponent<Rigidbody>().isKinematic = true;
            }
            else
            {
                Debug.LogError("[ZipLineInteractEffect] Player not found");
            }

            Trolley.transform.position = Spline.EvaluatePosition(0f);
            _timer = 0f;
            _currentState = State.Moving;
            MovePlayerToTrolley();
        }

        public override void OnInteractUpdate(float deltaTime)
        {
            switch (_currentState)
            {
                case State.Moving:
                    UpdateMoving(deltaTime);
                    break;
                case State.Returning:
                    UpdateReturning(deltaTime);
                    break;
            }
        }

        private void UpdateMoving(float deltaTime)
        {
            if (_targetPlayerObject == null) return;

            _timer += deltaTime;
            float t = Mathf.Clamp01(_timer / Duration);
            float evaluatedT = Mathf.Clamp01(SpeedCurve.Evaluate(t));
            Trolley.transform.position = Spline.EvaluatePosition(evaluatedT);
            MovePlayerToTrolley();

            if (t >= 1f)
            {
                // プレイヤーをここで降ろす
                GameInput.I.ToggleMoveInput(true);
                GameInput.I.ToggleActionInput(true);

                if (_targetPlayerObject != null)
                {
                    _targetPlayerObject.GetComponent<Rigidbody>().isKinematic = false;
                    _targetPlayerObject = null;
                }

                // Trolleyだけ始点へ戻すフェーズへ
                _timer = 0f;
                _currentState = State.Returning;
            }
        }

        private void UpdateReturning(float deltaTime)
        {
            _timer += deltaTime;
            float t = Mathf.Clamp01(_timer / ReturnDuration);

            float evaluatedT = SpeedCurve.Evaluate(t);
            Trolley.transform.position = Spline.EvaluatePosition(1f - evaluatedT);

            if (t >= 1f)
            {
                Trolley.transform.position = Spline.EvaluatePosition(0f);
                _activeEffect.EndInteract(); // ここで初めてInteractableBase側の終了処理を呼ぶ
            }
        }

        private void MovePlayerToTrolley()
        {
            if (_targetPlayerObject == null) return;
            _targetPlayerObject.transform.position = Trolley.transform.position + PlayerOffset;
        }

        public override void OnInteractEnd()
        {
            // Returning完了後にInteractableBase.EndInteract()経由で呼ばれる
            _currentState = State.Idle;

            if (_activeEffect != null)
            {
                _activeEffect.ForceSetInteractable = true; // ここで初めて使用可能に戻す
                _activeEffect = null;
            }
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new ZipLineInteractEffect
            {
                Spline = Spline,
                Trolley = Trolley,
                Duration = Duration,
                ReturnDuration = ReturnDuration,
                SpeedCurve = SpeedCurve,
                PlayerOffset = PlayerOffset
            };
        }
    }
}