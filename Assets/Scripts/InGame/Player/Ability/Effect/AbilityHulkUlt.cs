using System;
using System.Collections.Generic;
using InGame.Exhibit;
using InGame.Interact;
using September.Common;
using September.InGame.Effect;
using UnityEngine;
using Object = UnityEngine.Object;

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
        private IReadOnlyList<InteractableBase> _exhibits;

        protected override void OnCutInEnd()
        {
            Debug.Log("[AbilityHulkUlt] End");

            RequestEndAbility();
        }

        protected override void OnStartEffect()
        {
            var player = Parameter.Owner;

            _exhibits ??= GetExhibits();

            foreach (var item in _exhibits)
            {
                var sqDistance = (item.transform.position - player.transform.position).sqrMagnitude;
                if (sqDistance > _radius * _radius) continue;

                item.SetCooldown(_exhibitsCooldownTime);
            }

            _effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
            _effectSpawner?.RequestPlayOneShotEffect(_effectType, player.transform.position + player.transform.rotation * _effectOffset, Quaternion.identity);
        }

        private IReadOnlyList<InteractableBase> GetExhibits()
        {
            if (ExhibitRegistry.I != null)
            {
                return ExhibitRegistry.I.Items;
            }
            else
            {
                Debug.LogWarning("[AbilityHulkUlt] ExhibitRegistry が存在しないため、全探索を行います");
                return Object.FindObjectsByType<InteractableBase>(FindObjectsSortMode.None);
            }
        }
    }
}
