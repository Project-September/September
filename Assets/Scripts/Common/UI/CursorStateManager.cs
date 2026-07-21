using UnityEngine;

namespace September.Common
{
    public static class CursorStateManager
    {
        private static int _showRequestStack;

        public static void ShowCursor()
        {
            _showRequestStack++;
            Debug.Log($"[CursorStateManager] Show Requested. (currentStack: {_showRequestStack})");
        }

        public static void HideCursor()
        {
            _showRequestStack--;
            Debug.Log($"[CursorStateManager] Hide Requested. (currentStack: {_showRequestStack})");
        }

        public static void UpdateCursorState()
        {
            if (_showRequestStack < 0)
            {
                _showRequestStack = 0;
            }

            if (_showRequestStack > 0 && GameInput.I.UseDeviceType == GameInput.DeviceType.KeyboardMouse)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}
