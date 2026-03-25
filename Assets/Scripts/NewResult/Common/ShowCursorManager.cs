using UniRx;
using UnityEngine;

namespace September.NewResult
{
    public class ShowCursorManager : MonoBehaviour
    {
        private void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
            
            this.ObserveEveryValueChanged(_ => GameInput.I.UseDeviceType)
                .Subscribe(x =>
                {
                    if (x == GameInput.DeviceType.KeyboardMouse)
                    {
                        Cursor.visible = true;
                        Cursor.lockState = CursorLockMode.None;
                    }
                    else
                    {
                        Cursor.visible = false;
                        Cursor.lockState = CursorLockMode.Locked;
                    }
                });
        }
    }
}