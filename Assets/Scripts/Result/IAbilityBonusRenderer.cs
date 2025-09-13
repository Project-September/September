using TMPro;
using UnityEngine;

namespace Result
{
    public interface IAbilityBonusRenderer
    {
        int Render(ResultDataInbox inbox, Transform rowRoot, GameObject rowPrefab, TextMeshProUGUI abilityTitle);
    }
}