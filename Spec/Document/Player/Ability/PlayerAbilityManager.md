# PlayerAbilityManager.cs

## 概要
プレイヤーのアビリティを管理するクラス。入力条件とアビリティの実行を結びつけ、条件に応じてアビリティを開始・更新する。複数のアビリティと実行条件を保持し、ネットワーク同期された更新処理を行う。

## このクラスの責務
- 複数のアビリティ（AbilityBase）の保持と管理
- 複数の実行条件（IAbilityExecuteCondition）の保持と評価
- 入力（PlayerInput）の取得と更新
- 条件に応じたアビリティの開始
- 全アビリティの更新処理
- アビリティ名による検索・取得

## 使用される場所 / 呼び出し元
- プレイヤープレハブにアタッチされ、プレイヤーのアビリティシステムの中心として機能
- ネットワーク生成時に自動的に初期化（`Spawned()`）
- Photon Fusionの更新サイクル（`FixedUpdateNetwork()`）で自動的に実行

## 依存関係（重要なものだけ）

### 他のクラス
- `AbilityBase`: アビリティの基底クラス。このマネージャーが管理する
- `IAbilityExecuteCondition`: アビリティ実行条件のインターフェース。条件判定に使用
- `PlayerInput`: プレイヤーの入力データ（ボタン入力など）
- `NetworkObject`: ネットワーク同期されたオブジェクト（プレイヤー自身）
- `AbilityParameter`: アビリティに渡すパラメータ

### Photon Fusion の要素
- `NetworkBehaviour`: Photon Fusion のネットワーク同期ベースクラス
- `NetworkButtons`: ネットワーク同期されたボタン入力
- `PlayerInput`: プレイヤー入力の構造体（`GetInput<PlayerInput>()` で取得）

### Unity要素
- `SubclassSelector`: Inspector で派生クラスを選択可能にする属性
- `SerializeReference`: 抽象クラス・インターフェースの実装を Inspector で選択可能にする

## 主要フィールド / プロパティ

### シリアライズフィールド（Inspectorで設定）
- `_abilities`: 管理するアビリティのリスト（SubclassSelectorで派生クラスを選択）
- `_conditions`: アビリティ実行条件のリスト（SubclassSelectorで実装クラスを選択）

### プライベートフィールド
- `_previousButtons`: 前フレームのボタン入力
- `_currentButtons`: 現在のボタン入力
- `_networkObject`: このプレイヤーのNetworkObject
- `_abilityByName`: アビリティ名でアビリティを検索するための辞書

## 主要メソッド

### `Spawned()`（オーバーライド）
- ネットワーク生成時に呼ばれる初期化処理
- `NetworkObject` コンポーネントを取得
- 全アビリティを辞書に登録（クラス名をキーとして）

### `Update()`
- Unity の通常更新処理
- 全アビリティの `OnUpdateLocal()` を呼び出し
- ローカルプレイヤー専用の処理（エフェクト、カメラ演出など）を実行

### `FixedUpdateNetwork()`（オーバーライド）
- Photon Fusion のネットワーク同期された固定更新処理
- プレイヤー入力を取得
- 前フレームと現在のボタン入力を更新
- `HasStateAuthority` チェック（Host側でのみ以降の処理を実行）
- 全実行条件をチェックし、条件を満たしたアビリティを開始
- 全アビリティの `Tick()` を呼び出して更新

## ネットワーク同期（Fusion）

### Networked Property の同期内容
- このクラス自体には `[Networked]` プロパティはない
- 管理する `AbilityBase` 派生クラスでネットワーク同期が必要な場合は、そちらで実装

### 入力の取得
- `GetInput<PlayerInput>(out var input)` でプレイヤー入力を取得
- 入力が取得できない場合（入力権限がない場合）は処理をスキップ

### Host/Client で何が行われるか
- **全クライアント**:
  - `Update()` で全アビリティの `OnUpdateLocal()` を実行（ローカル処理）
  - `FixedUpdateNetwork()` で入力を取得し、ボタン状態を更新

- **Host側（HasStateAuthority = true）**:
  - 実行条件のチェック
  - 条件を満たしたアビリティの開始
  - 全アビリティの更新（`Tick()`）

- **Client側（HasStateAuthority = false）**:
  - 入力の取得と更新のみ実行
  - アビリティの開始・更新は行わない（Host側で実行される）

## ライフサイクル / 処理フロー

### 初期化フロー
1. プレイヤーがネットワーク生成される
2. `Spawned()` が呼ばれる
3. `NetworkObject` を取得
4. 全アビリティを辞書に登録

### 毎フレームの処理フロー（Update）
1. `Update()` が呼ばれる
2. 全アビリティの `OnUpdateLocal()` を実行（全クライアント）

### ネットワーク更新フロー（FixedUpdateNetwork）
1. `FixedUpdateNetwork()` が呼ばれる
2. プレイヤー入力を取得
3. 入力が取得できない場合は処理終了
4. 前フレームと現在のボタン入力を更新
5. **Host側のみ以降の処理を実行**:
   - 全実行条件をループ
     - 条件に対応するアビリティを辞書から取得
     - `TriggerEventContext` を作成（オーナー、アビリティ参照、現在と前のボタン）
     - 条件の `IsConditionMatch()` をチェック
     - 条件を満たしていればアビリティを開始（`Start()`）
   - 全アビリティの `Tick()` を呼び出して更新

## データの流れ

### 入力
- `PlayerInput`: プレイヤーの入力データ（ボタン、移動など）
- Inspector設定: アビリティリスト、実行条件リスト

### 処理
- 入力更新 → 条件評価 → アビリティ開始 → アビリティ更新

### 出力
- アビリティの開始・実行（攻撃、移動、エフェクトなど）
- ローカル処理（エフェクト、カメラ演出など）

## Inspector（公開フィールド）の説明

- `Abilities`: 管理するアビリティのリスト。SubclassSelectorで `AbilityBase` の派生クラスを選択して追加
  - 例: `AbilityNormalAttack`, `AbilityHammerAttack`, `AbilityMultiHitAttack`, `AbilityGrapplingHook`
- `Conditions`: アビリティ実行条件のリスト。SubclassSelectorで `IAbilityExecuteCondition` の実装クラスを選択して追加
  - 例: `AbilityNormalAttackCondition`, `AbilityHammerAttackCondition`, `SarutobiAttackCondition`

## 拡張ポイント

### 新しいアビリティの追加
1. `AbilityBase` を継承して新しいアビリティクラスを作成
2. Inspector の `Abilities` リストに追加（SubclassSelector経由）
3. 対応する実行条件を作成（必要に応じて）

### 新しい実行条件の追加
1. `IAbilityExecuteCondition` を実装して新しい条件クラスを作成
2. `TargetAbilityName` で対象のアビリティのクラス名を指定
3. `IsConditionMatch()` で条件判定ロジックを実装
4. Inspector の `Conditions` リストに追加（SubclassSelector経由）

### アビリティの取得
- `_abilityByName` 辞書を使用してクラス名でアビリティを検索可能
- 現在はprivateだが、publicプロパティを追加すれば外部からもアクセス可能

## 注意点 / 潜在するリスク

### アビリティ名の一致
- 実行条件の `TargetAbilityName` は**アビリティのクラス名**と完全一致する必要がある
- タイプミスや名前変更に注意（リファクタリング時にエラーが発生しやすい）
- 一致しない場合はエラーログが出力されるが、実行時エラーは発生しない

### HasStateAuthority チェック
- アビリティの開始・更新はHost側でのみ実行される
- Client側では入力の取得と更新のみ
- ネットワーク同期が必要な処理は、個々のアビリティクラスでRPCや[Networked]プロパティを使用して実装する必要がある

### 入力の取得失敗
- `GetInput<PlayerInput>()` が失敗した場合、ボタン状態の更新も行われない
- 入力権限がないプレイヤー（他プレイヤーのオブジェクト）では正常に動作する

### SubclassSelector の使用
- `SerializeReference` と `SubclassSelector` により、Inspector で柔軟にアビリティと条件を設定可能
- ただし、型安全性が弱いため、実行時エラーのリスクがある
- アビリティや条件の実装時に必ず動作確認が必要

### 辞書の初期化タイミング
- `_abilityByName` は `Spawned()` で初期化される
- それより前にアクセスすると例外が発生する可能性がある

## 今後の改善余地

コード内にTODOコメントは記載されていないが、以下の点が考えられる：

- **アビリティの動的追加・削除**: 実行時にアビリティを追加・削除する機能
- **アビリティ名の型安全化**: クラス名の文字列ではなく、型や列挙型で指定する仕組み
- **優先度システム**: 複数の条件が同時に満たされた場合の優先度制御
- **デバッグUI**: 現在のアビリティの状態をゲーム内で確認できるUI
- **アビリティの外部公開**: `_abilityByName` を公開して外部からアビリティを取得・制御できるようにする
- **条件の無効化**: 実行時に特定の条件を一時的に無効化する機能（スタン状態など）
