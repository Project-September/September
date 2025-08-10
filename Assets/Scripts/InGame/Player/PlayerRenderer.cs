using Fusion;
using UnityEngine;
using UnityEngine.Rendering;

namespace InGame.Player
{
    public class PlayerRenderer : NetworkBehaviour
    {
        [SerializeField] private Material _opticalCamouflageMaterial;
        [SerializeField] private Renderer[] _renderers;
        private Material[][] _defaultMaterials;
        public void Awake()
        {
            _defaultMaterials = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _defaultMaterials[i] = _renderers[i].materials;
            }
        }

        [Rpc]
        public void Rpc_StartOpticalCamouflage()
        {
            //  光学迷彩用のマテリアルに変更
            for (int i = 0; i < _renderers.Length; i++)
            {
                var mats = _renderers[i].materials;
                for (int j = 0; j < mats.Length; j++)
                {
                    mats[j] = _opticalCamouflageMaterial;
                }
                _renderers[i].materials = mats;
                _renderers[i].shadowCastingMode = ShadowCastingMode.Off;
            }
        }
        [Rpc]
        public void Rpc_StopOpticalCamouflage()
        {
            //  最初に割り当てられていたマテリアルに戻す
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].materials = _defaultMaterials[i];
                _renderers[i].shadowCastingMode = ShadowCastingMode.On;
            }
        }
    }
}