using Fusion;
using UnityEngine;

/// <summary> 嵐の管理を行うクラス </summary>
namespace September
{
    public class StormManager : NetworkBehaviour
    { 
        [SerializeField] private SkyboxChanger _skyboxChanger;
        [SerializeField] private ThunderFactory _thunderFactory;

        public void StartStorm()
        {
            _skyboxChanger.SkyBoxChange().Forget();
            _thunderFactory.ThunderSpawener();
        }
    }
}
