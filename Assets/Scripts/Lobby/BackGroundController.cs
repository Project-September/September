using UnityEngine;

public class BackGroundController : MonoBehaviour
{
    private GameInput _gameInput;
    private void Start()
    {
        _gameInput = GameInput.I;
    }

    private void Update()
    {
        if (_gameInput.Debug.Title.triggered)
        {
            gameObject.SetActive(false);
        }   
    }
}
