#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "TimeScale Controller")]
public class TimeScaleOverlay : Overlay
{
    const float kDefaultFixedDelta = 0.02f; // Unityデフォルト
    const float kMinScale = 0f;             // 0で完全停止も許可するなら0、最小0.1にしたいなら0.1fに
    const float kMaxScale = 10f;

    public override VisualElement CreatePanelContent()
    {
        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Row;
        root.style.alignItems = Align.Center;
        root.style.paddingLeft = 4;
        root.style.paddingRight = 4;

        // 再生直後に 0 になってしまう問題を回避
        float initial = Time.timeScale;
        if (initial <= 0f)
        {
            initial = 1f;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = kDefaultFixedDelta * Time.timeScale;
        }

        var valueLabel = new Label(initial.ToString("0.0"))
        {
            style =
            {
                unityTextAlign = TextAnchor.MiddleRight,
                width = 36
            }
        };

        // floatスライダー（0.0〜10.0）
        var slider = new Slider(kMinScale, kMaxScale)
        {
            value = initial,
            style =
            {
                width = 160
            }
        };

        // 0.1刻みで量子化
        slider.RegisterValueChangedCallback(evt =>
        {
            float quantized = Mathf.Round(evt.newValue * 10f) / 10f; // 0.1 step
            // もし最小0.1にしたい場合は Mathf.Max(0.1f, quantized)
            Time.timeScale = quantized;
            Time.fixedDeltaTime = kDefaultFixedDelta * Time.timeScale; // 物理も追従
            valueLabel.text = quantized.ToString("0.0");
            // スライダーの見た目も量子化値に寄せる（ドラッグ後の端数を消す）
            slider.SetValueWithoutNotify(quantized);
        });

        root.Add(valueLabel);
        root.Add(slider);
        return root;
    }
}
#endif