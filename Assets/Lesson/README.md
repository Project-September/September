# Lesson Exhibit Samples

## 足を速くする展示物

1. 展示物の `InteractableBase` の効果リストへ `LessonSpeedInteractEffect` を追加する。
2. `_speedMultiplier` を `5`、`_duration` を `10` に設定する。

生徒は `LessonSpeedInteractEffect.OnInteractStart` 内で `PlayerMovement` を取得し、`Speed` に倍率を代入する。効果時間が終わると速度は元に戻る。

## 見た目が変わる展示物

1. 展示物の `NetworkObject` と同じGameObjectへ `LessonVisualExhibit` を追加する。
2. `_targetRenderer` に色を変えたいRendererを設定する。
3. `InteractableBase` の効果リストへ `LessonVisualInteractEffect` を追加し、`_visualExhibit` を設定する。

生徒は `LessonVisualExhibit.ApplyLessonVisual` で、次のいずれか1行を有効にする。

```csharp
ApplyColorPulse(phase);
ApplyScalePulse(phase);
ApplySpin(phase);
```
