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
        [SerializeField] private GameObject[] _hideObjects;

        private Material[][] _defaultMaterials;

        private readonly Dictionary<Renderer, Material[]> _equipmentMaterials = new();

        private bool _isCamouflageEnabled;

        public void Awake()
        {
            _defaultMaterials = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _defaultMaterials[i] = _renderers[i].materials;
            }

            if (_playerEquipmentManager != null)
            {
                _playerEquipmentManager.Equipped += equipment =>
                {
                    if (_isCamouflageEnabled) AttachCamouflageMaterial(equipment);
                };
            }
        }

        [Rpc]
        public void Rpc_StartOpticalCamouflage()
        {
            // 装備中アイテムのマテリアルを取得
            AttachCamouflageMaterial(_playerEquipmentManager);

            //  光学迷彩用のマテリアルに変更
            for (int i = 0; i < _renderers.Length; i++)
            {
                AttachCamouflageMaterial(_renderers[i]);
            }

            SetObjectsActive(false);

            _isCamouflageEnabled = true;
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
            DetachCamouflageMaterial(_playerEquipmentManager);

            SetObjectsActive(true);

            _equipmentMaterials.Clear();
            _isCamouflageEnabled = false;
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

        private void AttachCamouflageMaterial(PlayerEquipmentManager playerEquipmentManager)
        {
            if (playerEquipmentManager == null)
            {
                Debug.LogWarning("Player Equipment Manager is null");
                return;
            }

            foreach (Equipment equipment in playerEquipmentManager.CurrentEquipments.Values)
            {
                AttachCamouflageMaterial(equipment);
            }
        }

        private void DetachCamouflageMaterial(PlayerEquipmentManager playerEquipmentManager)
        {
            if (playerEquipmentManager == null)
            {
                Debug.LogWarning("Player Equipment Manager is null");
                return;
            }

            foreach (Equipment equipment in playerEquipmentManager.CurrentEquipments.Values)
            {
                DetachCamouflageMaterial(equipment);
            }
        }

        private void AttachCamouflageMaterial(Equipment equipment)
        {
            if (equipment.ClonedObject == null) return;

            foreach (Renderer rend in equipment.ClonedObject.GetComponentsInChildren<Renderer>())
            {
                _equipmentMaterials.Add(rend, rend.materials);
                AttachCamouflageMaterial(rend);
            }
        }

        private void DetachCamouflageMaterial(Equipment equipment)
        {
            if (equipment.ClonedObject == null) return;

            foreach (Renderer rend in equipment.ClonedObject.GetComponentsInChildren<Renderer>())
            {
                if (!_equipmentMaterials.TryGetValue(rend, out Material[] defaultMaterials)) continue;

                rend.materials = defaultMaterials;
            }
        }

        private void SetObjectsActive(bool active)
        {
            foreach (GameObject obj in _hideObjects)
            {
                obj.SetActive(active);
            }
        }
    }
}
