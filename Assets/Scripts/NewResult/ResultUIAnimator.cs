using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace NewResult
{
    public class ResultUIAnimator : MonoBehaviour
    {
        [SerializeField] private float _showDuration = 0.3f;
        [SerializeField] private float _hideDuration = 0.3f;
        [SerializeField] private CanvasGroup _selectMenu;
        
        public async UniTask ShowResultUI()
        {
            var pos = _selectMenu.transform.position;
            _selectMenu.transform.position += Vector3.up * 200f;
            await _selectMenu.transform.DOMove(pos, _showDuration);
        }
    }
}