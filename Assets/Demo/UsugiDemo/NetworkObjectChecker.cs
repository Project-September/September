#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Fusion;

public class NetworkObjectChecker : MonoBehaviour
{
    [ContextMenu("Check NetworkObjects in Scene")]
    private void CheckSceneNetworkObjects()
    {
        var all = FindObjectsOfType<NetworkObject>();
        foreach (var netObj in all)
        {
            if (netObj == null)
            {
                Debug.LogWarning("Null NetworkObject found.");
                continue;
            }

            var components = netObj.GetComponents<NetworkBehaviour>();
            foreach (var comp in components)
            {
                if (comp == null)
                    Debug.LogError($"[Broken] {netObj.name} has missing NetworkBehaviour", netObj);
            }
        }
    }
}
#endif