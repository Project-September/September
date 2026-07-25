using System;
using InGame.Exhibit;
using September.Common;
using September.InGame.Effect;
using UnityEngine;

namespace InGame.Player.Ability.Effect
{
    [Serializable]
    public class AbilityHulkUlt : AbilityUltBase
    {
        [Header("必殺技効果設定")]
        [SerializeField] private float _radius = 20f;
        [SerializeField] private float _exhibitsCooldownTime = 5f;
        
        [Header("視覚効果設定")]
        [SerializeField] private EffectType _effectType;
        [SerializeField] private Vector3 _effectOffset;
        
        private EffectSpawner _effectSpawner;

        protected override void OnCutInEnd()
        {
            Debug.Log("[AbilityHulkUlt] End");

            RequestEndAbility();
        }

        protected override void OnStartEffect()
        {
            var player = Parameter.Owner;

            var items = ExhibitRegistry.I.Items;
            foreach (var item in items)
            {
                var sqDistance = (item.transform.position - player.transform.position).sqrMagnitude;
                if (sqDistance > _radius * _radius) continue;

                item.SetCooldown(_exhibitsCooldownTime);
            }

            _effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
            _effectSpawner?.RequestPlayOneShotEffect(_effectType, player.transform.position + player.transform.rotation * _effectOffset, Quaternion.identity);
        }
    }
}
