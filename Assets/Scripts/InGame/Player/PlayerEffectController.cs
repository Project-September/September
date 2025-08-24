using September.Common;
using September.InGame.Effect;
using UnityEngine;

public class PlayerEffectController : MonoBehaviour
{
    [SerializeField] private Transform _effectTransform;
    [SerializeField] private Vector3 _efffectRotationOffset;
    [SerializeField] private Vector3 _efffectPositionOffset;

    public void PlayPunchEffect()
    {
        var effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
        if (effectSpawner && _effectTransform)
            effectSpawner.RequestPlayOneShotEffect(EffectType.PunchImpact, _effectTransform.position + _efffectPositionOffset, _effectTransform.rotation * Quaternion.Euler(_efffectRotationOffset));
    }
}
