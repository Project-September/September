using Fusion;
using InGame.Exhibit;
using InGame.Interact;
using UnityEngine;

/// <summary> 海図のインタラクション効果を制御するクラス </summary>
public class NauticalChartBaseInteractEffect : CharacterInteractEffectBase
{
    [SerializeField] private NauticalChartInteractEffect _nauticalChartInteractEffect;

    private PlayerRef _interactPlayerRef;

    /// <summary> NauticalChartInteractEffectを複製して返す </summary>
    public override CharacterInteractEffectBase Clone()
    {
        return new NauticalChartBaseInteractEffect
        {
            _nauticalChartInteractEffect = _nauticalChartInteractEffect
        };
    }

    public override void OnInteractStart(IInteractableContext context, InteractableBase target)
    {
        _interactPlayerRef = PlayerRef.FromEncoded(context.Interactor);
        _nauticalChartInteractEffect.RPC_OnInteractStart(_interactPlayerRef);
        _nauticalChartInteractEffect.FixedUpdateNetwork();
    }
}
