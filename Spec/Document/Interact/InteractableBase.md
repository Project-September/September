# InteractableBase.cs

## 概要
プレイヤーがゲーム内でインタラクト（相互作用）できるオブジェクトの基底クラス。展示物などのインタラクト可能なオブジェクトに必要な、ネットワーク同期されたクールダウン管理、エフェクト再生、音声再生、バリデーション処理などの共通機能を提供する。

## このクラスの責務
- インタラクト実行時の基本処理フローの管理
- キャラクタータイプごとのインタラクト時間・クールダウン時間の管理
- インタラクト可否のバリデーション（クールダウン中、ゲーム終了状態、プレイヤースタン状態のチェック）
- エフェクトの再生（インタラクト完了エフェクト、クールダウンループエフェクト）
- 音声の再生（インタラクト音）
- 展示物タイプの登録とログ表示
- 派生クラスで個別の効果を実装するための拡張ポイントの提供

## 使用される場所 / 呼び出し元
コード内には直接的な呼び出し元は記載されていないが、一般的には以下から呼び出される想定：
- プレイヤーコントローラーからの `Interact()` メソッド呼び出し
- インタラクトトリガーシステム
- 派生クラス（具体的なインタラクト可能オブジェクト）が継承して利用

## 依存関係（重要なものだけ）

### 他のクラス
- `EffectSpawner`: エフェクト再生管理
- `PlayerDatabase`: プレイヤー情報の取得と展示物の登録
- `InGameManager`: ゲーム状態の確認（ゲーム終了判定など）
- `PlayerManager`: プレイヤーのスタン状態の確認
- `UIController`: UI表示制御（ログ表示、説明UIの変更）
- `StaticServiceLocator`: 各種サービスクラスの取得
- `CRIAudio`: サウンド再生

### インターフェース
- `IInteractableContext`: インタラクト時のコンテキスト情報を保持するインターフェース
- `CharacterInteractEffectBase`: キャラクターごとのインタラクト効果の基底クラス（SerializeReference で使用）

### Photon Fusion の要素
- `NetworkBehaviour`: Photon Fusion のネットワーク同期オブジェクト
- `[Networked]`: ネットワーク同期されるプロパティ
- `[Rpc]`: リモートプロシージャコール（RPC）メソッド
- `PlayerRef`: プレイヤーの参照
- `NetworkObject`: ネットワーク上のオブジェクト

### その他
- `AudioBroadcaster`: サウンド再生用コンポーネント（RequireComponent で必須化）
- `SerializableDictionary`: Unity Inspector で編集可能な辞書型
- `UniTask`: 非同期処理ライブラリ

## 主要フィールド / プロパティ

### シリアライズフィールド（Inspectorで設定）
- `_requiredInteractTimeDictionary`: キャラクタータイプごとのインタラクト所要時間
- `_cooldownTimeDictionary`: キャラクタータイプごとのクールダウン時間
- `_characterEffects`: キャラクタータイプごとのインタラクト効果リスト（SubclassSelectorで派生クラスを選択）
- `_type`: 展示物のタイプ（ExhibitType）
- `_interactEffectOffset`: インタラクトエフェクトの位置オフセット
- `_cooldownEffectTransform`: クールダウンエフェクトを再生するTransform（未設定の場合は自身）
- `_cooldownEffectOffset`: クールダウンエフェクトの位置オフセット
- `_cooldownEffectRotation`: クールダウンエフェクトの回転
- `_interactEffectType`: インタラクト完了時のエフェクトタイプ
- `_cooldownEffectType`: クールダウン中のエフェクトタイプ
- `_spawnCooldownEffectOnStart`: クールダウンエフェクトをインタラクト直後に再生するか
- `_audioBroadcaster`: サウンド再生用コンポーネント
- `_interactSoundCueName`: インタラクト時のサウンドCue名
- `_interactSoundTrackingType`: サウンドの再生タイプ（2D/3D）

### Networkedプロパティ（ネットワーク同期）
- `LastInteractTime`: 最後にインタラクトされた時刻
- `LastUsedCooldownTime`: 最後に使用されたクールダウン時間
- `ForceSetInteractable`: 外部から強制的にインタラクト可否を設定するフラグ

### その他のプロパティ
- `RequiredInteractTimeDictionary`: インタラクト所要時間の読み取り専用プロパティ
- `CooldownTimeDictionary`: クールダウン時間の読み取り専用プロパティ
- `ExhibitType`: 展示物タイプの読み取り専用プロパティ
- `AudioBroadcaster`: AudioBroadcasterの読み取り専用プロパティ

### プライベートフィールド
- `_activeEffectBase`: 現在アクティブなインタラクト効果

## 主要メソッド

### `Interact(IInteractableContext context)`
- インタラクトのメイン処理
- `HasStateAuthority` チェック（ホストのみ実行）
- バリデーション実行
- クールダウン時間の設定（CharacterType.All を優先、なければキャラ固有の値）
- インタラクト時刻の記録
- クールダウンエフェクトの再生
- インタラクトエフェクトの再生
- 派生クラスの `OnInteract()` 呼び出し
- PlayerDatabase への展示物登録
- インタラクト音の再生
- 全クライアントへのログ表示RPC

### `PlayCooldownEffect(float cooldownTime)`
- クールダウン中のループエフェクトを再生
- 指定時間経過後にエフェクトを停止
- エフェクト終了時にSE再生（Exhibit_Revive）

### `ValidateInteraction(IInteractableContext context)`
- インタラクト可否の共通バリデーション
- クールダウン中かチェック
- オブジェクトがアクティブかチェック
- `ForceSetInteractable` フラグのチェック
- 派生クラスの `OnValidateInteraction()` 呼び出し

### `OnValidateInteraction(IInteractableContext context, CharacterType charaType)`（仮想メソッド）
- 派生クラスで個別条件をチェックするための拡張ポイント
- デフォルトではゲーム終了状態とプレイヤースタン状態をチェック

### `IsGameEnded()`
- InGameManager から現在のゲーム状態を取得
- EndingState かどうかをチェック
- エラー時は安全側でインタラクトを許可（false を返す）

### `IsPlayerStunned(IInteractableContext context)`
- インタラクト実行者の PlayerRef を取得
- PlayerManager からスタン状態を確認
- エラー時は安全側でインタラクトを許可（false を返す）

### `OnInteract(IInteractableContext context)`（仮想メソッド）
- 派生クラスで個別のインタラクト処理を実装するための拡張ポイント
- キャラクタータイプに応じた効果（CharacterInteractEffectBase）を選択
- CharacterType.All を優先、なければキャラ固有の効果
- 効果をクローンして `OnInteractStart()` を呼び出し

### `IsInCooldown()`
- 現在クールダウン中かどうかを判定
- クールダウン時間が0以下の場合は常に false
- 現在時刻と最終インタラクト時刻の差分で判定

### `EndInteract()`
- インタラクト終了処理
- アクティブな効果の `OnInteractEnd()` を呼び出し
- 説明UIの変更RPC
- アクティブな効果をクリア

### RPCメソッド
- `Rpc_PlaySE(string sheet, string cueName, Vector3 position)`: 全クライアントでSEを再生
- `RPC_ChangeDescriptionUI(int mode)`: InputAuthority（入力権限を持つクライアント）の説明UIを変更
- `Rpc_ShowInteractLog(PlayerRef actor, ExhibitType exhibitType)`: 全クライアントにインタラクトログを表示

## ネットワーク同期（Fusion）

### Networked Property の同期内容
- `LastInteractTime`: インタラクト時刻をネットワーク同期。初期値は `-9999f`（過去の時刻）
- `LastUsedCooldownTime`: 使用されたクールダウン時間をネットワーク同期。初期値は `0f`
- `ForceSetInteractable`: インタラクト可否フラグをネットワーク同期。初期値は `true`

### RPC の役割
- `Rpc_PlaySE`: Host から全クライアントにSE再生を指示（RpcSources/RpcTargets指定なし = All）
- `RPC_ChangeDescriptionUI`: Host から InputAuthority（入力権限を持つクライアント）に説明UI変更を指示
- `Rpc_ShowInteractLog`: Host から全クライアントにログ表示を指示

### Host/Client で何が行われるか
- **Host側（HasStateAuthority = true）**:
  - `Interact()` メソッドでメイン処理を実行
  - バリデーション、クールダウン設定、エフェクト再生、効果発動、RPC送信
  - Update系メソッドでアクティブな効果を更新

- **Client側（HasStateAuthority = false）**:
  - `Interact()` は即座にリターン（処理しない）
  - Networkedプロパティの変更を受信して同期
  - RPCを受信してSE再生やUI更新を実行

## ライフサイクル / 処理フロー

### Update系メソッド
- `Update()`: HasStateAuthority チェック後、`_activeEffectBase?.OnInteractUpdate(Time.deltaTime)` 呼び出し
- `LateUpdate()`: HasStateAuthority チェック後、`_activeEffectBase?.OnInteractLateUpdate(Time.deltaTime)` 呼び出し
- `FixedUpdate()`: HasStateAuthority チェック後、`_activeEffectBase?.OnInteractFixedUpdate()` 呼び出し
- `FixedUpdateNetwork()`: Photon Fusion の固定更新。入力を取得して `_activeEffectBase?.OnInteractFixedNetworkUpdate(input)` 呼び出し

### コリジョンイベント
- `OnCollisionStay(Collision collision)`: HasStateAuthority チェック後、`_activeEffectBase?.OnInteractCollisionStay(collision)` 呼び出し

### 処理の流れ
1. 外部から `Interact()` 呼び出し（Host側のみ処理）
2. バリデーションチェック
3. クールダウン時間の設定と記録
4. エフェクト再生（クールダウン、インタラクト完了）
5. `OnInteract()` で派生クラスの処理実行
6. 展示物登録、音声再生、ログ表示
7. Update系メソッドで効果を継続的に更新
8. 適切なタイミングで `EndInteract()` を呼び出して終了

## データの流れ

### 入力
- `IInteractableContext`: インタラクト実行者（Interactor）とキャラクタータイプ（CharacterType）

### 処理
- バリデーション → クールダウン設定 → エフェクト再生 → 効果発動 → 展示物登録・音声再生・ログ表示

### 出力
- Networkedプロパティの更新（全クライアントに同期）
- RPC送信（SE再生、UI更新、ログ表示）
- EffectSpawner へのエフェクト再生リクエスト
- PlayerDatabase への展示物登録
- AudioBroadcaster へのサウンド再生リクエスト

## Inspector（公開フィールド）の説明

- `Required Interact Time Dictionary`: キャラクタータイプごとのインタラクト所要時間（秒）
- `Cooldown Time Dictionary`: キャラクタータイプごとのクールダウン時間（秒）
- `Character Effects`: キャラクタータイプごとのインタラクト効果リスト（SubclassSelectorで派生クラスを選択可能）
- `Type`: この展示物のタイプ（ExhibitType）
- `Interact Effect Offset`: インタラクトエフェクトの位置オフセット
- `Cooldown Effect Transform`: クールダウンエフェクトを再生するTransform（未設定の場合は自身のTransform）
- `Cooldown Effect Offset`: クールダウンエフェクトの位置オフセット
- `Cooldown Effect Rotation`: クールダウンエフェクトの回転（Euler角）
- `Interact Effect Type`: インタラクト完了時のエフェクトタイプ（デフォルト: NormalInteractComplete）
- `Cooldown Effect Type`: クールダウン中のエフェクトタイプ（デフォルト: CooldownSquare）
- `Spawn Cooldown Effect On Start`: クールダウンエフェクトをインタラクト直後に再生するか（デフォルト: true）
- `Audio Broadcaster`: サウンド再生用コンポーネント
- `Interact Sound Cue Name`: インタラクト時のサウンドCue名
- `Interact Sound Tracking Type`: サウンドの再生タイプ（デフォルト: Spot = 3D）

## 拡張ポイント

### 派生クラスで拡張する仮想メソッド
- `OnValidateInteraction(IInteractableContext context, CharacterType charaType)`: 個別のバリデーション条件を追加
- `OnInteract(IInteractableContext context)`: 個別のインタラクト処理を追加

### CharacterInteractEffectBase システム
- `_characterEffects` リストに効果クラスを追加することで、キャラクタータイプごとに異なる効果を設定可能
- 効果クラスは `CharacterInteractEffectBase` を継承して実装
- 効果クラスは Update系メソッドやコリジョンイベントのコールバックを受け取れる

### ForceSetInteractable プロパティ
- 外部から `ForceSetInteractable` を false に設定することで、インタラクトを強制的に無効化できる

## 注意点 / 潜在するリスク

### HasStateAuthority チェック
- インタラクト処理やUpdate系メソッドは Host（StateAuthority）側でのみ実行される
- Client側で処理を実行したい場合はRPCを使用する必要がある

### クールダウン時間の優先順位
- `CharacterType.All` が設定されている場合、キャラ固有の値より優先される
- 意図しない動作を防ぐため、設定時に注意が必要

### エラーハンドリング
- `IsGameEnded()` と `IsPlayerStunned()` はエラー時に安全側（インタラクトを許可）で処理する
- 本番環境でのエラーが隠蔽される可能性がある

### 効果のクローン
- `OnInteract()` で効果をクローンしているため、同じ効果を複数のインスタンスで使いまわすことはできない
- クローン処理のコストに注意

### UniTask.Forget()
- `PlayCooldownEffect()` を `Forget()` で呼び出しているため、例外が発生してもキャッチされない
- エラーハンドリングが必要な場合は `Forget()` の使用を避ける

## 今後の改善余地

コード内にTODOコメントは記載されていないが、以下の点が考えられる：

- **エラーロギング**: 現在コメントアウトされている `Debug.LogError` を適切に管理する仕組み
- **バリデーションの詳細化**: `ValidateInteraction()` でどの条件で拒否されたかを呼び出し元に返す仕組み
- **効果システムの型安全性**: `_characterEffects` の SerializeReference は柔軟だが、型チェックが弱いため実行時エラーのリスクがある
- **クールダウンの可視化**: クールダウン状態を外部から取得できるプロパティやイベントの追加
