using Cysharp.Threading.Tasks.Triggers;
using Fusion;
using UnityEngine;

namespace InGame.TreasureChest
{
    public abstract class ItemBase : NetworkBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.transform.root.gameObject.CompareTag("Player")) return;

            OnHitPlayer(other.transform.root.gameObject);
            Runner.Despawn(Object);
        }

        protected abstract void OnHitPlayer(GameObject player);
    }
}
