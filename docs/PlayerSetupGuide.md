# 新キャラクター・アビリティ作成ガイド

## 1. 新キャラクターの作成手順

### ステップ1: Prefabを複製
1. `Assets/Prefabs/` 以下にある既存のプレイヤーPrefabを複製
   - `PlayerBase` または `Sarutobi`、`Haru` などを複製
2. 新しい名前にリネーム

### ステップ2: Prefab構成の理解

```
PlayerPrefab (Root)
├── Root ─────── プレイヤー関係のスクリプトを付けるところ
├── Mesh ─────── キャラモデルのPrefabを入れるところ
├── Collider ─── キャラの当たり判定に使う
└── その他 ───── 基本的に気にしなくてOK
```

### ステップ3: キャラモデルの差し替え
- `Mesh` の子オブジェクトを新しいキャラクターモデルに差し替え

---

## 2. アビリティの作成・付け替え

### アビリティ追加の流れ

```
1. AbilityBaseを継承したクラスを作成
      ↓
2. IAbilityExecuteConditionを実装した実行条件クラスを作成
      ↓
3. PlayerAbilityManagerに登録
   - _abilities リストにアビリティを追加
   - _conditions リストに実行条件を追加
```

### アビリティクラスの作成例

```csharp
public class MyAbility : AbilityBase
{
    public override void OnStart()
    {
        // アビリティ発動時の処理
    }

    public override void OnUpdate()
    {
        // アビリティ実行中の毎フレーム処理（Host）
    }

    public override void OnUpdateLocal()
    {
        // アビリティ実行中の毎フレーム処理（ローカル）
    }
}
```

### 実行条件クラスの作成例

```csharp
public class MyAbilityCondition : IAbilityExecuteCondition
{
    public bool CanExecute(PlayerInput input, PlayerManager player)
    {
        // Ability1ボタンが押されたら発動
        return input.Buttons.IsSet(PlayerButtons.Ability1);
    }
}
```

### PlayerAbilityManagerへの登録
1. Prefabの `PlayerAbilityManager` コンポーネントを開く
2. `_abilities` リストに作成したアビリティを追加
3. `_conditions` リストに対応する実行条件を追加
   - リストの順番を合わせること（abilities[0] に対して conditions[0]）

---

## 3. アビリティのフェーズ

```
Available → Started → Active → Ending → Cooldown → Available
```

| フェーズ | 説明 |
|---------|------|
| Available | 発動可能状態 |
| Started | 発動開始（OnStart呼び出し） |
| Active | 実行中（OnUpdate/OnUpdateLocal呼び出し） |
| Ending | 終了処理中 |
| Cooldown | クールダウン中 |

---

## 4. 入力ボタン一覧

| PlayerButtons | 用途 |
|--------------|------|
| Attack | 攻撃 |
| Ability1 | アビリティ1 |
| Ability2 | アビリティ2 |
| Jump | ジャンプ |
| Dash | ダッシュ |
| Aim | エイム |
| Interact | インタラクト |

---

## 5. 既存アビリティの参考例

| クラス名 | 説明 |
|---------|------|
| `AbilityNormalAttack` | 通常攻撃 |
| `AbilityHammerAttack` | ハンマー攻撃 |
| `AbilityGrapplingHook` | グラップリングフック（猿飛） |
| `AbilityMultiHitAttack` | 多段攻撃 |

既存のアビリティを参考にして新しいアビリティを作成すると効率的です。
