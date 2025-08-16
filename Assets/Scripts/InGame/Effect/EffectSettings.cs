using UnityEngine;

namespace InGame.Effect
{
    public class EffectSettings : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _hitEffect;
        [SerializeField] private int _damage; 
        private void OnCollisionEnter(Collision collision)
        {
            if(!collision.gameObject.CompareTag("Ground"))
                return;
            
            var effect = Instantiate(_hitEffect, collision.contacts[0].point, Quaternion.identity);
            effect.Play();
            Destroy(effect,3f);
            Destroy(gameObject);
            
            // 当たったものがPlayerであればDamageを与える
        }
    }
}
