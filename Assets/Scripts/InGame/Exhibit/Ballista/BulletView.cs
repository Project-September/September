using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September
{
    public class BulletView : MonoBehaviour
    {
        [SerializeField] private GameObject[] _bulletObjectsArray;
        [SerializeField] private GameObject _reloadText;
        private List<GameObject> _bulletObjects = new();
        
        private void Start()
        {
            _bulletObjects = _bulletObjectsArray.ToList();
            _reloadText.SetActive(false);
        }
        
        public void UpdateAmmo(int currentAmmo, float reloadTime)
        {
            BulletUpdate(currentAmmo);
            if (currentAmmo == 0)
            {
                Reload(reloadTime).Forget();
            }
        }

        public async UniTask Reload(float time)
        {
            _reloadText.SetActive(true);
            await UniTask.WaitForSeconds(time);
            _reloadText.SetActive(false);
        }
        
        public void BulletUpdate(int currentBulletCount)
        {
            for (var i = 0; i < _bulletObjects.Count; i++)
            {
                _bulletObjects[i].SetActive(currentBulletCount > i);
            }
        }
    }
}
