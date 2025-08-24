using September.Common;
using September.InGame.Effect;
using UnityEngine;

public class PlayerEffectController : MonoBehaviour
{
    [SerializeField] private Transform _punchEffectTransform;
    [SerializeField] private Vector3 _punchEffectRotationOffset;
    [SerializeField] private Vector3 _punchEffectPositionOffset;
    [SerializeField] private Vector3 _stunEffectPositionOffset;
    private string _generateStunEffectId;

    public void PlayPunchEffect()
    {
        var effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
        if (effectSpawner && _punchEffectTransform)
            effectSpawner.RequestPlayOneShotEffect(EffectType.PunchImpact, _punchEffectTransform.position + _punchEffectPositionOffset, _punchEffectTransform.rotation * Quaternion.Euler(_punchEffectRotationOffset));
    }
    
    public void PlayStunEffect()
    {
        var effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
        if (effectSpawner && _punchEffectTransform)
        {
            _generateStunEffectId = System.Guid.NewGuid().ToString();
            effectSpawner.RequestPlayLoopEffect(_generateStunEffectId, EffectType.StunNormal, transform.position + _stunEffectPositionOffset, Quaternion.identity);
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
