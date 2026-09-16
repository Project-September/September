using UnityEngine;

namespace InGame.Player.Ability
{
    public class AbilityExcaliburAttack : AbilityNormalAttack
    {
        [SerializeField] private EffectType _attackEffectType;
        [SerializeField] private float _offsetY = 1.5f;

        protected override void OnStart()
        {
            base.OnStart();
            var attackRotation = Parameter.Owner.GetComponent<PlayerMovement>().Rigidbody.rotation;
            _effectSpawner.RequestPlayOneShotEffect(
                _attackEffectType,
                Parameter.Owner.transform.position + Vector3.up * _offsetY,
                attackRotation); ;
        }
    }
}
