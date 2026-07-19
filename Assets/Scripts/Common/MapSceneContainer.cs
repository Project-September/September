using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using InGame.Common;
using NaughtyAttributes;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace September.Common
{
    [CreateAssetMenu(menuName = "Scriptable Objects/MapSceneContainer")]
    public class MapSceneContainer : AssetsContainerBase<MapSceneContainer>
    {
        [SerializeField] private MapScene[] _maps;

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void Init()
        {
            // Tの内容を確定させてから呼び出す必要があるため、継承先で呼び出している
            // もっといい方法はあると思う
            Load().Forget();
        }

        public string GetMapSceneName(MapType mapType)
        {
            MapScene scene = _maps.FirstOrDefault(x => x.MapType == mapType);
            if (scene != null)
            {
                return scene.SceneName;
            }

            Debug.LogError($"No map found for map type {mapType}");
            return string.Empty;
        }

        [Serializable]
        private class MapScene
        {
            public MapType MapType;
            [Scene] public string SceneName;
        }
    }
}
