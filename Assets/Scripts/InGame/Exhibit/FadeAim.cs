using DG.Tweening;
using Fusion;
using UnityEngine;

public class FadeAim : NetworkBehaviour
{
    [Networked]
    private float NetworkedAlpha { get; set; }

    private Material _material;
    private Sequence _fadeSequence; // シーケンスを管理するための変数

    public override void Spawned()
    {
        base.Spawned();
        _material = GetComponent<Renderer>().material;

        if (HasStateAuthority)
        {
            NetworkedAlpha = 1.0f; 
        }
    }

    public override void Render()
    {
        // プレイヤーが乗り降りした際にSetActiveが切り替わるため、
        // そのタイミングでアニメーションを開始・停止する
        if (gameObject.activeInHierarchy)
        {
            // アクティブになったら、もしアニメーションがなければ開始する
            if (_fadeSequence == null || !_fadeSequence.IsActive())
            {
                StartFadeAnimation();
            }
        }
        else
        {
            // 非アクティブになったらアニメーションを停止する
            if (_fadeSequence != null && _fadeSequence.IsActive())
            {
                _fadeSequence.Kill();
            }
        }
    }

    // フェードインとフェードアウトを繰り返すアニメーションを開始する
    private void StartFadeAnimation()
    {
        if (_fadeSequence != null)
        {
            _fadeSequence.Kill();
        }

        _fadeSequence = DOTween.Sequence();
        _fadeSequence.SetLoops(-1);

        // より速いフェードイン
        _fadeSequence.Append(_material.DOFade(1.0f, 0.2f));

        // ほとんど待機しない
        _fadeSequence.AppendInterval(0.1f);

        // より速いフェードアウト
        _fadeSequence.Append(_material.DOFade(0.0f, 0.2f));

        // ほとんど待機しない
        _fadeSequence.AppendInterval(0.1f);
    }
}