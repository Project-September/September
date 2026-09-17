using UnityEngine;

namespace InGame.Player.Hatano
{
    /// <summary>
    /// ハタノのアニメーションイベントを管理する
    /// </summary>
    public class HatanoAnimationEventReceiver : MonoBehaviour
    {
        [SerializeField] private HatanoAbilityStatusManagement _abilityStatusManagement;

        public void OnChangeAnimationEnd()
        {
            _abilityStatusManagement.CompleteWeaponChangeAnimation();
        }
    }
}
