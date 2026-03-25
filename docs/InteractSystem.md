# Interactシステム調査結果

## ファイル構成

| ファイル | 役割 |
|---------|------|
| `Assets/Scripts/InGame/Interact/InteractableBase.cs` | すべてのインタラクト可能オブジェクトの基底クラス |
| `Assets/Scripts/InGame/Interact/PlayerInteractionController.cs` | プレイヤー側の検出・トリガー処理 |
| `Assets/Scripts/InGame/Interact/CharacterInteractEffectBase.cs` | インタラクト時のエフェクト基底クラス |
| `Assets/Scripts/InGame/Interact/Test/ExampleSwitch.cs` | テスト用インタラクタブル |
| `Assets/Scripts/InGame/Interact/Test/SimpleLogEffect.cs` | テスト用エフェクト |

---

## インタラクションの流れ

```
1. プレイヤーがInteractボタンを押す
      ↓
2. PlayerInteractionController が近くのオブジェクトを検出
   (OverlapSphere + 角度チェック)
      ↓
3. ボタン長押し中に _currentInteractTime を加算
      ↓
4. 必要時間に達したら CompleteInteraction()
      ↓
5. Host: 直接 Interact() を呼び出し
   Client: RPC_RequestInteract() で Host に依頼
      ↓
6. InteractableBase.Interact() で実行
   - バリデーション
   - クールダウン設定
   - エフェクト再生
   - OnInteract() で効果発動
```

---

## 主要クラス詳細

### InteractableBase (NetworkBehaviour)

すべてのインタラクト可能オブジェクトが継承する基底クラス。**Host/StateAuthority のみ**で実行される。

#### Networkedプロパティ
| プロパティ | 説明 |
|-----------|------|
| `LastInteractTime` | 最後にインタラクトした時刻 |
| `LastUsedCooldownTime` | 現在のクールダウン時間 |
| `ForceSetInteractable` | 外部からの有効/無効切り替え |

#### 主な設定フィールド
| フィールド | 説明 |
|-----------|------|
| `_requiredInteractTimeDictionary` | キャラクター別のインタラクト必要時間 |
| `_cooldownTimeDictionary` | キャラクター別のクールダウン時間 |
| `_characterEffects` | 発動するエフェクトのリスト |
| `_interactEffectType` / `_cooldownEffectType` | 視覚エフェクトの種類 |
| `_audioBroadcaster` / `_interactSoundCueName` | 音声設定 |
| `_type` (ExhibitType) | 分類（ログ/スコア用） |

#### 主要メソッド
| メソッド | 実行場所 | 説明 |
|---------|---------|------|
| `Interact(IInteractableContext)` | Host | メインパイプライン: 検証→クールダウン→エフェクト→OnInteract |
| `ValidateInteraction()` | Any | クールダウン、オブジェクト状態、スタン、ゲーム終了をチェック |
| `OnInteractStart()` | Host | virtual - カスタムインタラクト開始処理 |
| `OnValidateInteraction()` | Host | virtual - キャラクター別バリデーション |
| `OnInteract()` | Host | virtual - キャラクタータイプに応じたエフェクトを選択・実行 |
| `PlayCooldownEffect()` | Host | async - ループエフェクト生成、クールダウン待機、回復音再生 |
| `IsInCooldown()` | Any | クールダウン中かどうかを返す |
| `EndInteract()` | Any | クリーンアップ - エフェクトのOnInteractEnd呼び出し、UI リセット |

---

### PlayerInteractionController (NetworkBehaviour)

プレイヤーにアタッチされ、インタラクト対象を検出・トリガーする。

#### 設定フィールド
| フィールド | デフォルト値 | 説明 |
|-----------|------------|------|
| `_interactRadius` | 2.5m | 検出半径 |
| `_interactMask` | - | インタラクト可能なLayerMask |
| `_interactAngle` | 90° | 前方検出角度 |
| `_baseInteractTime` | 1.0s | 基本インタラクト時間 |
| `_ogreInteractMultiplier` | 1.0 | Ogreキャラクターの倍率 |
| `_interactResponseTimeout` | 3s | RPC応答待ちタイムアウト |

#### 検出ロジック (UpdateFocusedInteractable)
1. `Physics.OverlapSphere` で `_interactRadius` 内のオブジェクトを取得
2. `_interactMask` でフィルタリング
3. 距離と角度をバッファ付きで検証
4. 最も近い有効なターゲットを選択
5. バリデーション結果に基づいてUIを更新

#### インタラクト実行フロー
- **Hostの場合**: 直接 `InteractableBase.Interact(context)` を呼び出し
- **Clientの場合**: `RPC_RequestInteract()` でHostにリクエスト → 3秒タイムアウト

---

### CharacterInteractEffectBase (抽象クラス)

インタラクト完了時の効果を定義する抽象基底クラス。

#### 抽象メソッド（必須実装）
| メソッド | 説明 |
|---------|------|
| `OnInteractStart(IInteractableContext, InteractableBase)` | インタラクト完了時に呼ばれる |
| `Clone()` | 同じ設定のディープコピーを返す |

#### ライフサイクルメソッド（オプション）
| メソッド | 説明 |
|---------|------|
| `OnInteractUpdate(float deltaTime)` | 毎フレーム (Update) |
| `OnInteractLateUpdate(float deltaTime)` | 毎フレーム (LateUpdate) |
| `OnInteractFixedUpdate()` | 固定フレーム |
| `OnInteractFixedNetworkUpdate(PlayerInput)` | ネットワークティック毎（プレイヤー入力付き） |
| `OnInteractCollisionStay(Collision)` | 物理コリジョンコールバック |
| `OnInteractEnd()` | インタラクト終了時のクリーンアップ |

#### 重要な仕様
- `CharacterType` プロパティでどのキャラクターに適用するか指定（`CharacterType.All` で全キャラ対応）
- インタラクト時に**Clone**されて使用される（状態の汚染を防ぐ）

---

## ネットワーク同期

| 権限 | 役割 |
|-----|------|
| State Authority (Host) | `Interact()` 実行、エフェクト管理、バリデーション |
| Input Authority (Client) | 検出、ボタン入力、RPCでリクエスト送信 |
| 他クライアント | RPC経由で視覚・音声エフェクトを受信 |

### 使用されるRPC
| RPC | 方向 | 説明 |
|-----|------|------|
| `RPC_RequestInteract()` | Client→Host | インタラクトリクエスト |
| `RPC_ChangeDescriptionUI()` | StateAuth→InputAuth | UI更新 |
| `Rpc_ShowInteractLog()` | StateAuth→All | チャットログ表示 |
| `Rpc_PlaySE()` | StateAuth→All | クールダウン回復音 |

---

## 実装済みクラス一覧

### InteractableBase を継承
| クラス | 説明 |
|-------|------|
| `PropAirplane.cs` | 乗れる飛行機 |
| `ExampleSwitch.cs` | テスト用 |

### CharacterInteractEffectBase を継承
| クラス | 説明 |
|-------|------|
| `SimpleLogEffect` | テスト用（コンソールログ出力） |
| `CarInteractEffect` | 車インタラクションRPCトリガー |
| `WarpInteractEffect` | プレイヤーをテレポート |
| `MoaiInteractEffect` | アニメーション + ステータスUI表示 |
| `TutankhamenInteractEffect` | 時限スピードバフ、マスクエフェクト |
| `OpticalCamouflageInteractEffect` | プレイヤーを透明化 |
| `PterodactylInteractEffect` | プテラノドンに乗る |
| `TyrannoInteractEffect` | Tレックスに乗る |
| `MuramasaInteractEffect` | 武器エフェクト |
| `SateliteCannonInteractEffect` | キャノンインタラクション |
| `LondonTelephoneInteractEffect` | 電話インタラクション |
| `EquipCannonBallEffect` | キャノンボール装備 |
| `StradivariusInteractEffect` | 楽器エフェクト |

---

## 関連システムとの連携

| システム | 連携内容 |
|---------|---------|
| `PlayerManager` | `SetWarpTarget()` で位置更新、スタン状態チェック |
| `PlayerDatabase` | `Server_AddExhibit()` で展示物登録 |
| `UIController` | インタラクションUI、プログレスバー、ログ表示 |
| `EffectSpawner` | 視覚エフェクト再生（完了時、クールダウンループ） |
| `AudioBroadcaster` | インタラクションサウンド再生 |
| `FormationManager` | グループインタラクション（仲間にも効果適用） |
| `PlayerStatus/Health` | バフ/デバフ適用 |

---

## 主要Enum

### CharacterType
```csharp
None = 0
OkabeWright
HulkTheButcher
Tanihira
Sarutobi
All  // 全キャラクターに適用
```

### ExhibitType（スコア/ログ用）
```csharp
None, Ptr (Pterodactyl), TRex, Art, AirPlane,
FlagealCamouflage, Tutankhamun, LondonTelephone, Car,
Moai, SateliteCanon, Instrument, Muramasa
```

---

## 新しいインタラクトを追加する手順

1. `InteractableBase` を継承したクラスを作成
2. Inspector で設定:
   - `_requiredInteractTimeDictionary`: キャラクター別インタラクト時間
   - `_cooldownTimeDictionary`: キャラクター別クールダウン時間
3. `CharacterInteractEffectBase` を継承したエフェクトクラスを作成
   - `OnInteractStart()` と `Clone()` を実装
   - 必要に応じてライフサイクルメソッドをオーバーライド
4. Inspector で `_characterEffects` リストにエフェクトを追加
5. 必要に応じてオーバーライド:
   - `OnValidateInteraction()`: カスタムバリデーション
   - `OnInteract()`: 標準エフェクトシステムを使わない場合

---

## 設計パターン

| パターン | 適用箇所 |
|---------|---------|
| Strategy | CharacterInteractEffectBase でキャラクター別エフェクト切り替え |
| Template Method | OnValidateInteraction, OnInteract がオーバーライド可能なフック |
| Prototype (Clone) | エフェクトはClone()されて使用 |
| Service Locator | StaticServiceLocator で EffectSpawner, InGameManager にアクセス |
| RPC Authorization | 適切な RpcSources/RpcTargets でネットワークセキュリティ確保 |
