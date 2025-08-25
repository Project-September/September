using System;
using September.Common;
using September.InGame.Effect;
using UniRx;
using UnityEngine;

public class PlayerEffectController : MonoBehaviour
{
    [SerializeField] private ParticleSystem _punchEffect;
    [SerializeField] private Vector3 _stunEffectPositionOffset;
    private string _generateStunEffectId;
    private IDisposable _punchFollow;

    public void PlayPunchEffect()
    {
        var effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
        if (effectSpawner && _punchEffect)
        {
            _punchFollow?.Dispose();
            _punchEffect.Play();
            _punchFollow = Observable.EveryLateUpdate()
                .TakeWhile(_ => _punchEffect && _punchEffect.IsAlive(true) && _punchEffect.isPlaying)
                .Subscribe(_ =>
                {
                    var baseRot = Quaternion.LookRotation(transform.forward, Vector3.up);
                    _punchEffect.transform.rotation = baseRot;
                })
                .AddTo(this);
        }
    }

    public void PlayStunEffect()
    {
        var effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
        if (effectSpawner)
        {
            _generateStunEffectId = System.Guid.NewGuid().ToString();
            effectSpawner.RequestPlayLoopEffect(_generateStunEffectId, EffectType.StunNormal,
                transform.position + _stunEffectPositionOffset, Quaternion.identity);
        }
    }

    public void StopStunEffect()
    {
        var effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
        if (effectSpawner && !string.IsNullOrEmpty(_generateStunEffectId))
        {
            effectSpawner.StopEffect(_generateStunEffectId);
            _generateStunEffectId = string.Empty;
        }
    }
}