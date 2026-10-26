using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerEquipmentManager : NetworkBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private Equipment[] _equipmentData;

        private readonly Dictionary<HumanBodyBones, Equipment> _currentEquipments = new();
        private GameObject _currentEquipmentObject;
        private bool _isEquipmentVisible = true;

        public IReadOnlyDictionary<HumanBodyBones, Equipment> CurrentEquipments => _currentEquipments;

        public event Action<Equipment> Equipped;
        public event Action<Equipment> Unequipped;

        /// <summary>
        /// 現在装備しているオブジェクトのメッシュ表示を全クライアントで切り替える。
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_SetCurrentEquipmentVisible(bool visible)
        {
            SetCurrentEquipmentVisible(visible);
        }

        /// <summary>
        /// 現在装備しているオブジェクトのメッシュ表示をローカルで切り替える。
        /// MeshRendererとSkinnedMeshRendererを含む、装備配下のすべてのRendererが対象。
        /// </summary>
        public void SetCurrentEquipmentVisible(bool visible)
        {
            _isEquipmentVisible = visible;
            SetEquipmentVisible(_currentEquipmentObject, visible);
        }

        private void Start()
        {
            foreach (var equipment in _equipmentData)
            {
                if (equipment.StartingEquipment)
                {
                    ChangeEquipment(equipment);
                }
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_ChangeEquipment(EquipmentType type)
        {
            var equipment = _equipmentData.FirstOrDefault(x => x.Type == type);

            if (equipment == null)
            {
                Debug.LogError($"[EquipmentManager] EquipmentType '{type}' was not found in EquipmentData.");
                return;
            }

            ChangeEquipment(equipment);
        }

        private void ChangeEquipment(Equipment targetEquipment)
        {
            if (_currentEquipments.TryGetValue(targetEquipment.HumanBodyBones, out var currentEquipment))
            {
                DestroyEquipment(currentEquipment);
            }

            InstanceEquipment(targetEquipment);

            _currentEquipments[targetEquipment.HumanBodyBones] = targetEquipment;

            Equipped?.Invoke(targetEquipment);
        }

        private void InstanceEquipment(Equipment targetEquipment)
        {
            if (targetEquipment.Prefab == null) return;

            var parent = _animator.GetBoneTransform(targetEquipment.HumanBodyBones);
            var clonedEquipment = Instantiate(targetEquipment.Prefab, parent);

            clonedEquipment.transform.localPosition = targetEquipment.PositionOffset;
            clonedEquipment.transform.localEulerAngles = targetEquipment.RotationOffset;

            targetEquipment.ClonedObject = clonedEquipment;
            _currentEquipmentObject = clonedEquipment;
            SetEquipmentVisible(clonedEquipment, _isEquipmentVisible);
        }

        private static void SetEquipmentVisible(GameObject equipmentObject, bool visible)
        {
            if (!equipmentObject) return;

            foreach (var equipmentRenderer in equipmentObject.GetComponentsInChildren<Renderer>(true))
            {
                equipmentRenderer.enabled = visible;
            }
        }

        private void DestroyEquipment(Equipment equipment)
        {
            var equipmentObject = _currentEquipmentObject
                ? _currentEquipmentObject
                : equipment.ClonedObject;

            if (!equipmentObject) return;

            Destroy(equipmentObject);
            _currentEquipmentObject = null;
            equipment.ClonedObject = null;

            Unequipped?.Invoke(equipment);
        }
    }
    [System.Serializable]
    public class Equipment
    {
        [SerializeField] private EquipmentType _type;
        public EquipmentType Type => _type;
        [SerializeField] private GameObject _prefab;
        public GameObject Prefab => _prefab;
        [SerializeField] private Vector3 _positionOffset;
        public Vector3 PositionOffset => _positionOffset;
        [SerializeField] private Vector3 _rotationOffset;
        public Vector3 RotationOffset => _rotationOffset;
        [SerializeField] private HumanBodyBones _humanBodyBones;
        public HumanBodyBones HumanBodyBones => _humanBodyBones;
        [SerializeField] private bool _startingEquipment;
        public bool StartingEquipment => _startingEquipment;

        [HideInInspector] public GameObject ClonedObject;
    }
    public enum EquipmentType
    {
        NormalAttack, Armory, Tutankhamen
    }
}
