using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.Player;
using UnityEngine;

namespace InGame.TreasureChest
{
    public class InvincibleItem : ItemBase
    {
        [SerializeField] private float _buffDuration;
        protected override void OnHitPlayer(GameObject player)
        {
            if (player.TryGetComponent(out PlayerHealth health))
            {
                InvincibleAsync(health, player.GetCancellationTokenOnDestroy()).Forget();
            }
        }

        private async UniTask InvincibleAsync(PlayerHealth playerHealth, CancellationToken token)
        {
            playerHealth.IsItemInvincible = true;
            await UniTask.WaitForSeconds(_buffDuration, cancellationToken: token);
            playerHealth.IsItemInvincible = false;
        }
    }
}
