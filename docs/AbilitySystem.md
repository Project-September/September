# アビリティシステム技術資料

## 目次
1. [概要](#概要)
2. [ディレクトリ構造](#ディレクトリ構造)
3. [コアクラス](#コアクラス)
4. [実行フロー](#実行フロー)
5. [各アビリティの詳細仕様](#各アビリティの詳細仕様)
6. [ネットワーク同期](#ネットワーク同期)
7. [新規アビリティ追加手順](#新規アビリティ追加手順)

---

## 概要

本プロジェクトのアビリティシステムは、プレイヤーの攻撃やスキルを管理するフレームワークです。
主な特徴:
- **フェーズベースの状態管理**: 明確なライフサイクル（Available → Started → Active → Ending → Cooldown）
- **条件駆動型**: 入力条件とアビリティを分離し、柔軟な組み合わせが可能
- **ネットワーク対応**: Photon Fusion によるHost権限モデルで同期

---

## ディレクトリ構造

```
Assets/Scripts/InGame/Player/Ability/
├── PlayerAbilityManager.cs              # アビリティシステムの統括管理
├── Effect/
│   ├── AbilityBase.cs                   # アビリティの基底クラス
│   ├── AbilityNormalAttack.cs           # 通常攻撃
│   ├── AbilityHammerAttack.cs           # ハンマー攻撃（通常攻撃の派生）
│   └── AbilityMultiHitAttack.cs         # マルチヒット攻撃（通常攻撃の派生）
└── Condition/
    ├── IAbilityExecuteCondition.cs      # 実行条件インターフェース
    ├── AbilityNormalAttackCondition.cs  # 通常攻撃の実行条件
    ├── AbilityHammerAttackCondition.cs  # ハンマー攻撃の実行条件
    └── SarutobiAttackCondition.cs       # Sarutobi攻撃の実行条件

Assets/Scripts/InGame/Player/Sarutobi/
└── AbilityGrapplingHook.cs              # グラップリングフック（独自実装）
```

---

## コアクラス

### AbilityBase（基底クラス）

**ファイル**: `Assets/Scripts/InGame/Player/Ability/Effect/AbilityBase.cs`

すべてのアビリティの基底クラス。フェーズ管理とライフサイクルを提供する。

#### フェーズ（AbilityPhase）

```csharp
public enum AbilityPhase
{
    Available,   // 使用可能な待機状態
    Started,     // 開始された直後（1フレームのみ）
    Active,      // 実行中（OnUpdate()が毎フレーム呼ばれる）
    Ending,      // 終了処理中（1フレームのみ）
    Cooldown,    // クールダウン中
}
```

#### 主要プロパティ

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `_cooldown` | float | クールダウン時間（秒） |
| `_phase` | AbilityPhase | 現在のフェーズ |
| `Parameter` | AbilityParameter | オーナー（NetworkObject）情報 |
| `Phase` | AbilityPhase | 現在のフェーズ（読み取り専用） |

#### 主要メソッド

| メソッド | 説明 |
|---------|------|
| `Start(AbilityParameter)` | アビリティを開始（PlayerAbilityManagerから呼び出し） |
| `Tick(float deltaTime)` | 毎フレーム呼び出される更新メソッド |
| `CanStartAbilityOverride()` | 使用可否判定（派生クラスでオーバーライド可能） |
| `OnStart()` | 開始処理（派生クラスでオーバーライド） |
| `OnUpdate(float deltaTime)` | 実行中の更新処理（派生クラスでオーバーライド） |
| `OnUpdateLocal(float deltaTime, GameObject)` | ローカル専用処理（エフェクト、デバッグ描画など） |
| `OnEndAbility()` | 終了処理 |

---

### PlayerAbilityManager（管理クラス）

**ファイル**: `Assets/Scripts/InGame/Player/Ability/PlayerAbilityManager.cs`

アビリティと入力条件を結びつけ、アビリティの開始・更新を管理する。

#### 主要プロパティ

```csharp
// Inspector で SubclassSelector を使って設定
[SerializeReference, SubclassSelector] private List<AbilityBase> _abilities;
[SerializeReference, SubclassSelector] private List<IAbilityExecuteCondition> _conditions;
```

#### 処理の流れ

1. **Spawned()**: アビリティを辞書に登録（クラス名をキーとして）
2. **Update()**: 全アビリティの `OnUpdateLocal()` を呼び出し（ローカル処理）
3. **FixedUpdateNetwork()**: 入力取得 → 条件チェック → アビリティ開始・更新

---

### IAbilityExecuteCondition（実行条件インターフェース）

**ファイル**: `Assets/Scripts/InGame/Player/Ability/Condition/IAbilityExecuteCondition.cs`

```csharp
public interface IAbilityExecuteCondition
{
    // 対象となるアビリティのクラス名（例: "AbilityNormalAttack"）
    public string TargetAbilityName { get; }

    // 条件が満たされているかを判定
    public bool IsConditionMatch(in TriggerEventContext context);
}
```

#### TriggerEventContext（条件判定用の構造体）

```csharp
public readonly struct TriggerEventContext
{
    public GameObject Owner { get; }              // プレイヤーのGameObject
    public AbilityBase AbilityRef { get; }        // 対象アビリティの参照
    public NetworkButtons CurrentButtons { get; } // 現在のボタン入力
    public NetworkButtons PreviousButtons { get; }// 前フレームのボタン入力
}
```

---

### PlayerInput / PlayerButtons（入力定義）

**ファイル**: `Assets/Scripts/Common/InputProvider.cs`

```csharp
public enum PlayerButtons
{
    Jump,            // ジャンプ
    Dash,            // ダッシュ
    Interact,        // インタラクト
    Attack,          // 攻撃
    Aim,             // エイム
    Ability1,        // アビリティ1
    Ability2,        // アビリティ2
    Warp,            // ワープ
    AirplaneForward, // 飛行機前進
    AirPlaneBack     // 飛行機後進
}

public struct PlayerInput : INetworkInput
{
    public NetworkButtons Buttons;           // ボタン入力状態
    public Vector2 MoveDirection;            // 移動方向
    public float CameraYaw;                  // カメラのYaw角
    public Vector3 DesiredLookDirection;     // 望む向き
}
```

---

## 実行フロー

```
┌─────────────────────────────────────────────────────────┐
│ 入力収集（InputProvider.OnInput）                       │
│ - PlayerInput 構造体に入力を格納                        │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ PlayerAbilityManager.FixedUpdateNetwork()               │
│                                                         │
│ 1. GetInput<PlayerInput>() で入力取得                   │
│ 2. ボタン状態を更新（前フレーム、現在フレーム）         │
│ 3. Host のみ以下を実行（HasStateAuthority）             │
│    - 全 IAbilityExecuteCondition をチェック             │
│    - 条件マッチしたアビリティを Start()                 │
│ 4. 全アビリティに Tick() を呼び出し                     │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ AbilityBase.Tick(deltaTime)                             │
│                                                         │
│ switch(_phase)                                          │
│ ├─ Started   → OnStart() → Active                      │
│ ├─ Active    → OnUpdate(deltaTime)                     │
│ ├─ Ending    → OnEndAbility() → Cooldown               │
│ └─ Cooldown  → クールダウン終了で Available に戻る      │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ PlayerAbilityManager.Update()                           │
│ - 全アビリティの OnUpdateLocal() を呼び出し             │
│ - ローカル専用処理（エフェクト、デバッグ描画）          │
└─────────────────────────────────────────────────────────┘
```

### フェーズ遷移図

```
Available ─[条件成立]→ Started ─[1フレーム後]→ Active
                                                 │
                                         [終了条件成立]
                                                 ↓
Cooldown ←─[クールダウン開始]─ Ending ←─────────┘
    │
    └─[クールダウン終了]→ Available（ループ）
```

---

## 各アビリティの詳細仕様

### AbilityNormalAttack（通常攻撃）

**ファイル**: `Assets/Scripts/InGame/Player/Ability/Effect/AbilityNormalAttack.cs`

プレイヤーの基本的な近接攻撃。

#### パラメータ

| パラメータ | デフォルト値 | 説明 |
|-----------|-------------|------|
| `_attackDamage` | 10 | 通常時のダメージ |
| `_ogreAttackDamage` | 15 | 鬼状態時のダメージ |
| `_startHitCheckFrame` | 17 | ヒット判定開始フレーム |
| `_endHitCheckFrame` | 21 | ヒット判定終了フレーム |
| `_endAttackFrame` | 22 | 攻撃終了フレーム |
| `_searchRadius` | 2f | 自動エイムの敵検索範囲 |
| `_boxHalfExtents` | (0.45, 0.85, 0.45) | ヒットボックスのハーフサイズ |
| `_boxCastDistance` | 1.0f | BoxCast の掃引距離 |

#### 主要機能

- **自動エイム**: 指定範囲内の最も近い敵を自動で狙う
- **BoxCast判定**: 矩形判定で複数敵に同時ヒット可能
- **二度当たり防止**: HashSet で同一攻撃中の重複ヒットを防止
- **移動制御**: 攻撃中は前方に移動、攻撃後に移動入力を復帰

#### 実行条件（AbilityNormalAttackExecuteCondition）

```csharp
return _playerMovement.IsGround           // 地面にいる
    && !_playerManager.IsStun             // スタン状態ではない
    && !IsGameEnded()                     // ゲーム進行中
    && _playerManager.CurrentPlayerControlState == PlayerManager.PlayerControlState.Normal
    && context.AbilityRef.Phase == AbilityBase.AbilityPhase.Available
    && context.AbilityRef.CanStartAbilityOverride()
    && context.CurrentButtons.GetPressed(context.PreviousButtons).IsSet(PlayerButtons.Attack);
```

---

### AbilityHammerAttack（ハンマー攻撃）

**ファイル**: `Assets/Scripts/InGame/Player/Ability/Effect/AbilityHammerAttack.cs`

AbilityNormalAttack を継承し、オブジェクト破壊機能を追加。

#### 追加機能

- **DisableInteractEffect の破壊**: `DisableInteractEffect` コンポーネントを持つオブジェクトを破壊可能
- **ボーナス記録**: PlayerDatabase に破壊したオブジェクト情報を記録

---

### AbilityMultiHitAttack（マルチヒット攻撃）

**ファイル**: `Assets/Scripts/InGame/Player/Ability/Effect/AbilityMultiHitAttack.cs`

AbilityNormalAttack を継承した連続攻撃（Sarutobi用）。

#### 追加パラメータ

```csharp
[Serializable]
struct AttackFrame
{
    public int Start;  // 開始フレーム
    public int End;    // 終了フレーム
}

[SerializeField] private AttackFrame[] _attackFrames; // 2回目以降の攻撃フレーム
```

#### 特徴

- 複数のヒット窓を定義可能
- 各段階でヒットリストをクリアし、再度ヒット可能に

---

### AbilityGrapplingHook（グラップリングフック）

**ファイル**: `Assets/Scripts/InGame/Player/Sarutobi/AbilityGrapplingHook.cs`

Sarutobi キャラクター専用の移動スキル。**AbilityBase を継承せず、独自実装**。

#### 状態管理

```csharp
public enum AbilityStateType { Ready, Active, Cooldown }

private enum GrappleStateType
{
    Shot,      // 射出アニメーション中
    ShotWait,  // ワイヤー到達待ち
    PreJump,   // ジャンプ準備
    Jumping,   // 移動中
    Landing    // 着地中
}
```

#### 主要パラメータ

| パラメータ | 説明 |
|-----------|------|
| `_distanceRange` | 有効距離範囲（Min/Max） |
| `_maxAngle` | 最大角度（カメラ正面からの） |
| `_wireSpeed` | ワイヤー速度 |
| `_pullingSpeed` | プレイヤー移動速度 |
| `_cooldown` | クールダウン時間 |

#### 実行フロー

```
Ready → [Ability1ボタン押下] → Active
  └→ Shot → ShotWait → PreJump → Jumping → Landing
       ↓
Cooldown → [クールダウン終了] → Ready
```

#### ネットワーク同期

- **RPC_GrappleStart**: InputAuthority から全クライアントへ開始通知
- **RPC_DisplayWireStart/End**: StateAuthority から全クライアントへワイヤー表示制御
- **Networked プロパティ**: `AbilityState`, `Cooldown` で状態同期

---

## ネットワーク同期

### Host権限モデル（HasStateAuthority）

```csharp
public override void FixedUpdateNetwork()
{
    // 入力は全クライアントで取得
    if (!GetInput<PlayerInput>(out var input)) return;

    // アビリティの開始・更新は Host のみ実行
    if (!HasStateAuthority) return;

    // 条件チェックとアビリティ開始...
}
```

### シミュレーション時刻の使用

```csharp
// NetworkRunner があればシミュレーション時刻、なければローカル時刻
CooldownEndTime = Runner ? Runner.SimulationTime + _cooldown : Time.time + _cooldown;
```

### ローカル専用処理の分離

| メソッド | 実行タイミング | 用途 |
|---------|---------------|------|
| `OnUpdate()` | FixedUpdateNetwork | ネットワーク同期が必要な処理（ダメージ、移動） |
| `OnUpdateLocal()` | Update | ローカル専用処理（エフェクト、デバッグ描画） |

### RPC パターン（GrapplingHook の例）

```csharp
// InputAuthority（操作者）から全クライアントへ
[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
void RPC_GrappleStart(Vector3 targetPosition) { ... }

// StateAuthority（Host）から全クライアントへ
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
void RPC_DisplayWireStart() { ... }
```

---

## 新規アビリティ追加手順

### 1. AbilityBase を継承したクラスを作成

```csharp
using System;
using UnityEngine;

namespace InGame.Player.Ability
{
    [Serializable]
    public class AbilityNewSkill : AbilityBase
    {
        [Header("新スキルパラメータ")]
        [SerializeField] private float _parameter1;
        [SerializeField] private int _parameter2;

        protected override void OnStart()
        {
            // 開始処理（アニメーション再生など）
        }

        protected override void OnUpdate(float deltaTime)
        {
            // 実行中の更新処理
            // 終了条件が満たされたら:
            // _phase = AbilityPhase.Ending;
        }

        public override void OnUpdateLocal(float deltaTime, GameObject owner)
        {
            // ローカル専用処理（エフェクト、デバッグ描画）
        }
    }
}
```

### 2. IAbilityExecuteCondition を実装した条件クラスを作成

```csharp
using System;
using Fusion;

namespace InGame.Player.Ability
{
    [Serializable]
    public class AbilityNewSkillCondition : IAbilityExecuteCondition
    {
        public string TargetAbilityName => nameof(AbilityNewSkill);

        // キャッシュ用
        private PlayerMovement _playerMovement;
        private PlayerManager _playerManager;

        public bool IsConditionMatch(in TriggerEventContext context)
        {
            if (!context.Owner) return false;
            if (!_playerMovement) _playerMovement = context.Owner.GetComponent<PlayerMovement>();
            if (!_playerManager) _playerManager = context.Owner.GetComponent<PlayerManager>();

            // 条件を定義
            return context.AbilityRef.Phase == AbilityBase.AbilityPhase.Available
                && context.AbilityRef.CanStartAbilityOverride()
                && context.CurrentButtons.GetPressed(context.PreviousButtons)
                    .IsSet(PlayerButtons.Ability1); // 使用するボタンを指定
        }
    }
}
```

### 3. PlayerAbilityManager の Inspector で登録

1. プレイヤーの Prefab を開く
2. `PlayerAbilityManager` コンポーネントを選択
3. `_abilities` リストに新しいアビリティを追加（SubclassSelector で選択）
4. `_conditions` リストに新しい条件を追加（SubclassSelector で選択）

### 注意事項

- **クラスに `[Serializable]` 属性を付ける**: Inspector で表示するために必須
- **nameof() を使用**: ターゲット名は文字列ではなく `nameof()` で指定し、リファクタリング耐性を確保
- **コンポーネントのキャッシュ**: 条件クラス内で毎フレーム `GetComponent` を呼ばないようにキャッシュする
- **フェーズ遷移の明示**: `OnUpdate()` 内で終了条件が満たされたら `_phase = AbilityPhase.Ending;` を設定

---

## 参照ファイル一覧

| ファイル | 説明 |
|---------|------|
| `Assets/Scripts/InGame/Player/Ability/PlayerAbilityManager.cs` | アビリティ管理 |
| `Assets/Scripts/InGame/Player/Ability/Effect/AbilityBase.cs` | 基底クラス |
| `Assets/Scripts/InGame/Player/Ability/Effect/AbilityNormalAttack.cs` | 通常攻撃 |
| `Assets/Scripts/InGame/Player/Ability/Effect/AbilityHammerAttack.cs` | ハンマー攻撃 |
| `Assets/Scripts/InGame/Player/Ability/Effect/AbilityMultiHitAttack.cs` | マルチヒット攻撃 |
| `Assets/Scripts/InGame/Player/Ability/Condition/IAbilityExecuteCondition.cs` | 条件インターフェース |
| `Assets/Scripts/InGame/Player/Ability/Condition/AbilityNormalAttackCondition.cs` | 通常攻撃条件 |
| `Assets/Scripts/InGame/Player/Sarutobi/AbilityGrapplingHook.cs` | グラップリングフック |
| `Assets/Scripts/Common/InputProvider.cs` | 入力定義 |
