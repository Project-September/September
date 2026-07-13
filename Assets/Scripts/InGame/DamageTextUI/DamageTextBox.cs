using UnityEngine;

public class DamageTextBox : MonoBehaviour
{
    private Vector3 _worldPosition;
    private Vector3 _randomOffset;
    private Camera _mainCamera;

    private void Update()
    {
        Vector3 screenPosition = _mainCamera.WorldToScreenPoint(_worldPosition);
        transform.position = screenPosition + _randomOffset;
    }

    public void GenerateValueBox
        (Vector3 worldPosition, Vector3 randomOffset, Camera camera)
    {
        _worldPosition = worldPosition;
        _randomOffset = randomOffset;
        _mainCamera = camera;
    }
}
