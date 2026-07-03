using UnityEngine;
using Fusion;
using InGame.Exhibit;
using InGame.Interact;
using September.Common;
using September.InGame.Common;

public class SharkInteractable : MountableExhibitBase
{
    [Header("SharkMovementProcessing"), SerializeField] private SharkMovementProcessing _movementProcessing;
    [Header("攻撃のクールダウンタイム"), SerializeField] private float _cooldownTime;
        
    /// <summary>
    /// インタラクション中か
    /// <para>true：インタラクション中　false：インタラクション中でない</para>
    /// </summary>
    [Networked] public bool IsSharkInteracting { get; private set; }
    /// <summary>
    /// 攻撃中か
    /// <para>true：攻撃中　false：待機中</para>
    /// </summary>
    [Networked] private bool IsAttacking { get; set; }
    
    private InteractableBase _interactableBase;
    
    private PlayerInput _playerInput;
    private float _deltaTime;
    private float _cooldownTimer; // 攻撃のクールダウンタイマー
    private float _attackAnimationFrame; // 攻撃アニメーションの現在のフレーム
        
    public override void Spawned()
    {
        base.Spawned();
        _interactableBase = GetComponent<InteractableBase>();
    }
        
    public override void GetOn(PlayerRef playerRef)
    {
        base.GetOn(playerRef);
        IsSharkInteracting = true;
        _interactableBase.ForceSetInteractable = false;
        // 攻撃状態の初期化
        _cooldownTimer = _cooldownTime;
        _attackAnimationFrame = 0;
        _movementProcessing.UpdatePositionBeforeWaterFall(transform.position);
    }
        
    public override void GetOff(PlayerRef playerRef)
    {
        base.GetOff(playerRef);
        _interactableBase.ForceSetInteractable = true;

        // インタラクトしていたプレイヤーを取得し、海に落ちる直前の位置に移動させる
        var obj = StaticServiceLocator.Instance.Get<InGameManager>()
            .PlayerDataDic[playerRef];
        obj.transform.position = _movementProcessing.PositionBeforeWaterFall;
    }

    public override void FixedUpdateNetwork()
    {
        // 移動処理関連
        _movementProcessing.CheckGroundManual(Rigidbody);
        var moveDirection = _movementProcessing.Move(_playerInput, Rigidbody);
        moveDirection.y = 0;
        _movementProcessing.Rotate(_deltaTime, moveDirection);
        _movementProcessing.AdsorptionOnGround(_deltaTime, Rigidbody);
        _movementProcessing.UpdatePositionBeforeWaterFall(transform.position);
    }

    public override void OnInteractFixedUpdate(PlayerInput playerInput, float deltaTime)
    {
        base.OnInteractFixedUpdate(playerInput, deltaTime);
            
        _playerInput = playerInput;
        _deltaTime = deltaTime;
            
        // ここから攻撃処理関連
        _cooldownTimer = Mathf.Min(_cooldownTime, _cooldownTimer + deltaTime);
        AttackStartTrigger(playerInput, OwnerPlayerRef);　// 攻撃開始
        OnAttackUpdate(deltaTime);　//Attack中にだけ発火するメソッド
    }
        
    /// <summary>
    /// 攻撃入力
    /// </summary>
    /// <param name="playerInput">プレイヤーの入力</param>
    /// <param name="playerRef">プレイヤー</param>
    private void AttackStartTrigger(PlayerInput playerInput, PlayerRef playerRef)
    {
        // 攻撃ボタンが押されていない場合、処理を行わない
        if (!playerInput.Buttons.IsSet(PlayerButtons.Attack)) return;
        AttackCoolDownCheck();
        CreateHitBox(playerRef);
    }
        
    /// <summary>
    /// クールダウンを確認し、攻撃開始フラグを立てる
    /// </summary>
    private void AttackCoolDownCheck()
    {
        if (_cooldownTimer < _cooldownTime) return;
        IsAttacking = true;
        _cooldownTimer = 0;
    }
        
    /// <summary>
    /// 攻撃アニメーションのフレームを更新
    /// </summary>
    /// <param name="deltaTime"></param>
    private void OnAttackUpdate(float deltaTime)
    {
        // 攻撃中のみ更新
        if (!IsAttacking) return;
            
        // フレームを進め、攻撃判定が有効なフレームだけヒット判定
        _attackAnimationFrame++;
        if (_attackAnimationFrame >= StartFrame && _attackAnimationFrame <= EndFrame)
        {
            Executor?.Tick(deltaTime);
        }
        
        // 攻撃終了のフレームを超えたら、ヒットボックスを破棄する
        if (!(_attackAnimationFrame >= EndFrame)) return;
        Executor?.Init();
            
        // 攻撃状態をの期化
        _attackAnimationFrame = 0;
        IsAttacking = false;
        Executor = null;
    }
        
    private void OnTriggerEnter(Collider other)
    {
        // Seaは仮の海のタグで付けているので、あとで変更を行う
        if (other.CompareTag("Sea"))
        {
            IsSharkInteracting = false;
        }
    }
}
