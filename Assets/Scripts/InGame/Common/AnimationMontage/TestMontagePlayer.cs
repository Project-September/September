using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace InGame.Common.AnimationMontage
{
    public class TestMontagePlayer : MonoBehaviour
    {
        [SerializeField] private MontagePlayer _player;
        [SerializeField] private AnimationMontage _montage;

        private void Start()
        {
            //PlayMontage().Forget();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                var ids = _player.GetActiveMontageId();
                foreach (var id in ids)
                {
                    //Debug.Log(id);
                }
                PlayMontage().Forget();
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                _player.StopMontage(_montage.Id);
            }
        }

        async UniTask PlayMontage()
        {
            var montage = await MontageRegistry.LoadAsync(_montage.Id);
            var handle = _player.PlayMontage(montage).OnComplete(() => Debug.Log("OnComplete")).OnKill(() => Debug.Log("OnKill"));
            try
            {
                await handle.ToUniTask();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Cancel");
                return;
            }
            
            Debug.Log("Complete");
        }

        public void TestNotify()
        {
            Debug.Log("Call Notify");
        }
    }
}
