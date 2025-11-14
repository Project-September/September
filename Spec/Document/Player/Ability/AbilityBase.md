# AbilityBase.cs

## 概要
プレイヤーのアビリティ（特殊能力や攻撃）の基底クラス。アビリティの状態管理（フェーズ管理）、開始・更新・終了・クールダウンの基本的なライフサイクルを提供する。派生クラスで個別のアビリティ動作を実装する。

## このクラスの責務
- アビリティのフェーズ管理（Available → Started → Active → Ending → Cooldown → Available）
- アビリティの開始・更新・終了処理の制御
- クールダウン時間の管理
- 派生クラスで個別のアビリティ動作を実装するための拡張ポイントの提供
- アビリティ実行可否の判定

## 使用される場所 / 呼び出し元
- `PlayerAbilityManager`: アビリティを管理し、条件に応じて開始・更新処理を呼び出す
- 派生クラス（`AbilityNormalAttack`, `AbilityHammerAttack`, `AbilityMultiHitAttack`, `AbilityGrapplingHook` など）が継承して個別のアビリティを実装

## 依存関係（重要なものだけ）

### 他のクラス
- `AbilityParameter`: アビリティのパラメータ（オーナーのNetworkObjectなど）を保持
- `NetworkRunner`: Photon Fusion のネットワークランナー（シミュレーション時刻の取得）

### Photon Fusion の要素
- `NetworkRunner`: シミュレーション時刻の取得
- `NetworkObject`: アビリティのオーナー（プレイヤー）の参照

## 主要フィールド / プロパティ

### シリアライズフィールド（Inspectorで設定）
- `_cooldown`: クールダウン時間（秒）
- `_phase`: 現在のアビリティのフェーズ（初期値: Available）

### プロテクテッドプロパティ
- `Runner`: NetworkRunnerのインスタンス（Instances.FirstOrDefault()で取得）

### パブリックプロパティ
- `Parameter`: アビリティのパラメータ（開始時に設定される）
- `Phase`: 現在のアビリティのフェーズ（読み取り専用）

### プライベートプロパティ
- `CooldownEndTime`: クールダウン終了時刻

## 主要メソッド

### `Start(AbilityParameter parameter)`
- アビリティを開始する
- パラメータを設定し、フェーズを `Started` に変更
- `PlayerAbilityManager` から呼び出される

### `Tick(float deltaTime)`
- アビリティを更新する
- 内部で `ProcessPhase()` を呼び出してフェーズごとの処理を実行
- `PlayerAbilityManager` から毎フレーム呼び出される

### `ProcessPhase(float deltaTime)`（プライベート）
- 現在のフェーズに応じて処理を振り分ける
- **Started**: `OnStart()` を呼び出して `Active` に遷移
- **Active**: `OnUpdate(deltaTime)` を呼び出して継続処理
- **Ending**: `OnEndAbility()` を呼び出して終了処理
- **Cooldown**: `OnCooldown()` を呼び出してクールダウン判定

### `CanStartAbilityOverride()`（仮想メソッド）
- アビリティが使用可能かどうかを判定
- デフォルトでは常に `true` を返す
- 派生クラスで独自の条件を追加可能（例: スタミナ、装備条件など）

### `OnStart()`（仮想メソッド）
- アビリティ開始時の処理
- 派生クラスでオーバーライドして実装

### `OnUpdate(float deltaTime)`（仮想メソッド）
- アビリティのActive中の更新処理
- 派生クラスでオーバーライドして実装

### `OnUpdateLocal(float deltaTime, GameObject owner)`（仮想メソッド）
- ローカルプレイヤー専用の更新処理
- 基本的には使用しない（コメントに記載あり）
- エフェクトやカメラ演出など、ローカル専用の処理が必要な場合に使用

### `OnEndAbility()`（仮想メソッド）
- アビリティ終了時の処理
- クールダウン終了時刻を計算し、フェーズを `Cooldown` に変更
- 派生クラスでオーバーライドして追加の終了処理を実装可能

### `OnCooldown()`（プロテクテッド）
- クールダウン中の処理
- 現在時刻がクールダウン終了時刻を超えたら `Available` に遷移

## ネットワーク同期（Fusion）

### Networked Property の同期内容
- このクラス自体には `[Networked]` プロパティは存在しない
- ネットワーク同期が必要な場合は派生クラスで実装

### Host/Client で何が行われるか
- このクラス自体にはネットワーク処理は含まれない
- `PlayerAbilityManager` が `HasStateAuthority` をチェックし、Host側でのみアビリティを開始・更新する
- NetworkRunnerの有無をチェックし、ある場合は `Runner.SimulationTime` を、ない場合は `Time.time` を使用
  - これにより、ローカルテスト（NetworkRunnerなし）でも動作可能

## ライフサイクル / 処理フロー

### アビリティのフェーズ遷移
1. **Available**: アビリティが使用可能な待機状態
2. **Started**: `Start()` が呼ばれた直後の状態（1フレームのみ）
3. **Active**: `OnStart()` が呼ばれた後の実行中状態。`OnUpdate()` が毎フレーム呼ばれる
4. **Ending**: アビリティを終了する状態（1フレームのみ）。`OnEndAbility()` が呼ばれる
5. **Cooldown**: クールダウン中の状態。時間経過で `Available` に戻る
6. **Available**: 再び使用可能になる（1に戻る）

### 処理の流れ
1. 条件クラスが条件を満たすと `PlayerAbilityManager` が `Start()` を呼び出し
2. フェーズが `Started` に変更される
3. 次のフレームで `Tick()` → `ProcessPhase()` → `OnStart()` が呼ばれ、フェーズが `Active` に
4. Active中は毎フレーム `OnUpdate()` が呼ばれる
5. 派生クラスが適切なタイミングでフェーズを `Ending` に変更
6. 次のフレームで `OnEndAbility()` が呼ばれ、クールダウン時刻が設定され、フェーズが `Cooldown` に
7. クールダウン中は毎フレーム `OnCooldown()` で時刻をチェック
8. クールダウン終了時刻を超えたらフェーズが `Available` に戻る

## データの流れ

### 入力
- `AbilityParameter`: アビリティのパラメータ（オーナーのNetworkObject）
- `deltaTime`: フレーム間の経過時間

### 処理
- フェーズ管理 → 各フェーズに応じた処理実行 → クールダウン管理

### 出力
- フェーズの変更
- 派生クラスが実装する個別の処理（攻撃、移動、エフェクトなど）

## 拡張ポイント

### 派生クラスで実装すべき仮想メソッド
- `CanStartAbilityOverride()`: アビリティ使用可否の独自条件
- `OnStart()`: アビリティ開始時の処理（アニメーション開始、エフェクト生成など）
- `OnUpdate(float deltaTime)`: アビリティ実行中の更新処理（攻撃判定、移動処理など）
- `OnUpdateLocal(float deltaTime, GameObject owner)`: ローカル専用の更新処理（カメラ演出など）
- `OnEndAbility()`: アビリティ終了時の処理（base.OnEndAbility()を呼び出してクールダウン開始）

### フェーズの制御
- 派生クラスは `_phase` を直接変更することで、フェーズを制御可能
- 例: `OnUpdate()` 内で条件を満たしたら `_phase = AbilityPhase.Ending;` としてアビリティを終了

## 注意点 / 潜在するリスク

### フェーズ遷移のタイミング
- `Started` と `Ending` フェーズは1フレームのみ存在する短命なフェーズ
- これらのフェーズでの処理は次のフレームで即座に次のフェーズに遷移する

### NetworkRunner の有無チェック
- `Runner` は `NetworkRunner.Instances.FirstOrDefault()` で取得されるため、ネットワークセッション外では `null` になる
- クールダウン時刻の計算時に `Runner` の有無をチェックし、なければ `Time.time` を使用
- これによりローカルテスト環境でも動作可能だが、ネットワーク同期はされない

### クールダウンの計算
- クールダウン終了時刻は `OnEndAbility()` で計算される
- 派生クラスで `OnEndAbility()` をオーバーライドする場合、`base.OnEndAbility()` を呼び出さないとクールダウンが開始されない

### パラメータのアクセスタイミング
- `Parameter` は `Start()` が呼ばれた後に設定される
- それより前にアクセスしないこと（コメントに記載あり）

## 今後の改善余地

コード内にTODOコメントは記載されていないが、以下の点が考えられる：

- **フェーズ変更イベント**: フェーズ変更時のイベント・コールバックの追加
- **キャンセル機能**: アビリティをキャンセルする仕組み（Activeから直接Availableに戻るなど）
- **ネットワーク同期対応**: 基底クラスでネットワーク同期の仕組みを提供（現在は派生クラスに任されている）
- **デバッグ情報**: フェーズ遷移のログやデバッグ情報の出力
- **エラーハンドリング**: `Runner` が途中でnullになった場合のエラーハンドリング
