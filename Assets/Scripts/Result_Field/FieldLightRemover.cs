using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September.Result_Field
{
    public class FieldLightRemover : MonoBehaviour
    {
        private static readonly int Alpha = Shader.PropertyToID("_Alpha");
        private static readonly int EmissivePower = Shader.PropertyToID("_EmissivePower");

        async void Start()
        {
            await UniTask.Yield(); // 初期化待機
            
            var renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            
            foreach (var r in renderers)
            {
                r.lightmapIndex = -1;
            }
            
            LightmapSettings.lightmaps = null;
        
            foreach (var m in renderers.SelectMany(x => x.materials))
            {
                if (m.HasFloat(Alpha))
                {
                    m.SetFloat(Alpha, 0);
                }
            
                if (m.HasFloat(EmissivePower))
                {
                    m.SetFloat(EmissivePower, 0);
                }
            }
        }
    }
}
