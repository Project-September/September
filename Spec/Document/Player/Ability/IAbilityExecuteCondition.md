# IAbilityExecuteCondition.cs

## 概要
アビリティ実行条件を定義するインターフェース。プレイヤーの入力やゲーム状態を元に、どのアビリティをいつ発動するかの条件を判定する。`TriggerEventContext` 構造体も含まれており、条件判定に必要な情報を保持する。

## このクラスの責務
- アビリティ実行条件のインターフェース定義
- 対象アビリティの指定（TargetAbilityName）
- 条件判定ロジックの定義（IsConditionMatch）
- 条件判定に必要なコンテキスト情報の提供（TriggerEventContext）

## 使用される場所 / 呼び出し元
- `PlayerAbilityManager`: 実行条件リストを保持し、毎フレーム条件をチェック
- 実装クラス（`AbilityNormalAttackCondition`, `AbilityHammerAttackCondition`, `SarutobiAttackCondition` など）が具体的な条件判定を実装

## 依存関係（重要なものだけ）

### 他のクラス
- `AbilityBase`: 条件が対象とするアビリティの基底クラス
- `PlayerAbilityManager`: このインターフェースを使用して条件評価を行う

### Photon Fusion の要素
- `NetworkButtons`: ネットワーク同期されたボタン入力

### Unity要素
- `GameObject`: プレイヤーオブジェクト（オーナー）

## 主要フィールド / プロパティ

### IAbilityExecuteCondition インターフェース

#### `TargetAbilityName`（プロパティ）
- 対象となるアビリティのクラス名（文字列）
- `PlayerAbilityManager` がこの名前でアビリティを検索する
- 例: `"AbilityNormalAttack"`, `"AbilityHammerAttack"`

#### `IsConditionMatch(in TriggerEventContext context)`（メソッド）
- 条件が満たされているかを判定
- `TriggerEventContext` を受け取り、条件を評価して bool を返す
- true を返すとアビリティが開始される

## 主要メソッド

### `IsConditionMatch(in TriggerEventContext context)`
- 条件判定のメインロジック
- 実装クラスで具体的な条件を定義
- 典型的な条件例:
  - 特定のボタンが押された（ButtonDown）
  - アビリティがAvailableフェーズである
  - プレイヤーが地面にいる
  - スタミナが十分にある
  - 前提となる他のアビリティが完了している

## TriggerEventContext 構造体

### 概要
条件判定に必要な情報を保持する読み取り専用構造体。`PlayerAbilityManager` から条件クラスに渡される。

### フィールド（すべて読み取り専用プロパティ）

#### `Owner`（GameObject）
- アビリティを実行するプレイヤーのGameObject
- プレイヤーの状態（地面にいるか、スタミナなど）を確認する際に使用

#### `AbilityRef`（AbilityBase）
- 対象アビリティの参照
- アビリティの現在のフェーズ（Phase）を確認する際に使用
- 多くの条件で「アビリティがAvailableフェーズか」をチェック

#### `CurrentButtons`（NetworkButtons）
- 現在のフレームのボタン入力状態
- どのボタンが押されているかを確認

#### `PreviousButtons`（NetworkButtons）
- 前フレームのボタン入力状態
- ボタンが「今押された」（ButtonDown）を判定する際に使用
- `CurrentButtons` と `PreviousButtons` を比較することで、エッジ検出が可能

### コンストラクタ
- 4つのパラメータを受け取り、各プロパティに設定
- `PlayerAbilityManager` 内で毎フレーム生成される

## ネットワーク同期（Fusion）

### Networked Property の同期内容
- このインターフェース・構造体自体にはネットワーク同期プロパティはない
- `NetworkButtons` は Photon Fusion でネットワーク同期される入力データ

### Host/Client で何が行われるか
- このインターフェースを使用する `PlayerAbilityManager` は、Host側（HasStateAuthority）でのみ条件評価を行う
- Client側では条件判定は実行されない
- 結果的に、アビリティの開始もHost側でのみ行われる

## ライフサイクル / 処理フロー

### 条件判定の流れ
1. `PlayerAbilityManager.FixedUpdateNetwork()` が呼ばれる（Host側のみ）
2. 全実行条件をループ
3. 各条件について:
   - `TargetAbilityName` で対象アビリティを取得
   - `TriggerEventContext` を作成（Owner, AbilityRef, CurrentButtons, PreviousButtons）
   - `IsConditionMatch(context)` を呼び出し
   - true が返されたらアビリティを開始

## データの流れ

### 入力
- `TriggerEventContext`: オーナー、アビリティ参照、ボタン入力（現在・前フレーム）

### 処理
- 条件判定ロジック（実装クラスで定義）

### 出力
- bool値（true: 条件を満たす → アビリティ開始、false: 条件を満たさない）

## 拡張ポイント

### 新しい実行条件の作成方法
1. `IAbilityExecuteCondition` を実装した新しいクラスを作成
2. `TargetAbilityName` プロパティで対象アビリティのクラス名を返す
3. `IsConditionMatch()` メソッドで条件判定ロジックを実装

### 実装例（疑似コード）
```csharp
[Serializable]
public class MyAbilityCondition : IAbilityExecuteCondition
{
    public string TargetAbilityName => "MyAbility";

    public bool IsConditionMatch(in TriggerEventContext context)
    {
        // アビリティがAvailableフェーズか
        if (context.AbilityRef.Phase != AbilityBase.AbilityPhase.Available)
            return false;

        // 攻撃ボタンが今押されたか（エッジ検出）
        bool attackButtonDown = context.CurrentButtons.IsSet(PlayerButton.Attack)
                             && !context.PreviousButtons.IsSet(PlayerButton.Attack);

        return attackButtonDown;
    }
}
```

### TriggerEventContext の活用
- `Owner`: プレイヤーの状態確認（`GetComponent<>()` で他のコンポーネントを取得）
- `AbilityRef`: アビリティのフェーズやプロパティを確認
- `CurrentButtons` / `PreviousButtons`: ボタン入力の判定（押下、離した、長押しなど）

## 注意点 / 潜在するリスク

### TargetAbilityName の一致
- `TargetAbilityName` は**アビリティのクラス名**と完全一致する必要がある
- 文字列ベースのため、タイプミスやリファクタリング時の名前変更に注意
- 一致しない場合、`PlayerAbilityManager` でエラーログが出力されるが、実行時エラーにはならない

### 条件判定の頻度
- `IsConditionMatch()` は毎フレーム（FixedUpdateNetwork）呼ばれる
- 重い処理を入れるとパフォーマンスに影響
- キャッシュや最適化を検討

### in パラメータ
- `IsConditionMatch(in TriggerEventContext context)` の `in` キーワードは参照渡しの最適化
- 構造体のコピーを避けてパフォーマンスを向上
- 条件内で `context` を変更することはできない（読み取り専用）

### NetworkButtons の使用
- `NetworkButtons` は Photon Fusion 特有の型
- ボタンの状態確認には `.IsSet(PlayerButton.XXX)` などのメソッドを使用
- エッジ検出（ButtonDown/ButtonUp）は自分で実装する必要がある

## 今後の改善余地

コード内にTODOコメントは記載されていないが、以下の点が考えられる：

- **型安全なアビリティ指定**: 文字列ではなく、型やenumでアビリティを指定する仕組み
- **条件の組み合わせ**: AND/OR/NOTなどの論理演算で複数条件を組み合わせる機能
- **条件の優先度**: 複数の条件が同時に満たされた場合の優先度制御
- **デバッグ情報**: 条件が満たされなかった理由をログ出力する機能
- **TriggerEventContext の拡張**: プレイヤーの状態（地面にいるか、スタミナなど）を直接含める
- **条件の再利用性**: 共通の条件判定ロジック（「ボタンが今押された」など）をユーティリティ関数化
