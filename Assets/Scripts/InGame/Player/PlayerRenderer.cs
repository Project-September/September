using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Rendering;

namespace InGame.Player
{
    public class PlayerRenderer : NetworkBehaviour
    {
        [SerializeField] private Material _opticalCamouflageMaterial;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private PlayerEquipmentManager _playerEquipmentManager;

        private Material[][] _defaultMaterials;

        private readonly Dictionary<Renderer, Material[]> _equipmentMaterials = new();

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
            // 装備中アイテムのマテリアルを取得
            foreach (Equipment equipment in _playerEquipmentManager.CurrentEquipments.Values)
            {
                AttachCamouflageMaterial(equipment);
            }

            //  光学迷彩用のマテリアルに変更
            for (int i = 0; i < _renderers.Length; i++)
            {
                AttachCamouflageMaterial(_renderers[i]);
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

            //  装備中のアイテムのマテリアルを戻す
            foreach (Equipment equipment in _playerEquipmentManager.CurrentEquipments.Values)
            {
                DetachCamouflageMaterial(equipment);
            }

            _equipmentMaterials.Clear();
        }

        private void AttachCamouflageMaterial(Renderer rend)
        {
            var mats = rend.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = _opticalCamouflageMaterial;
            }
            rend.materials = mats;
            rend.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void AttachCamouflageMaterial(Equipment equipment)
        {
            foreach (Renderer rend in equipment.ClonedObject.GetComponentsInChildren<Renderer>())
            {
                _equipmentMaterials.Add(rend, rend.materials);
                AttachCamouflageMaterial(rend);
            }
        }

        private void DetachCamouflageMaterial(Equipment equipment)
        {
            foreach (Renderer rend in equipment.ClonedObject.GetComponentsInChildren<Renderer>())
            {
                if (!_equipmentMaterials.TryGetValue(rend, out Material[] defaultMaterials)) continue;

                rend.materials = defaultMaterials;
            }
        }
    }
}
