using System.Threading;
using September.Common;
using UnityEngine;

public class ResultManager : MonoBehaviour
{
    [SerializeField] private int _reslutTime;
    private CancellationTokenSource _cts;
    private GameInput _gameInput;
    private void Start()
    {
        _cts = new CancellationTokenSource();
        _gameInput =  GameInput.I;
    }

    private void Update()
    {
        if (_gameInput.Debug.Result.triggered)
        {
            QuitGame();
        }
    }

    private void QuitGame()
    {
        NetworkManager.Instance.QuitLobby().Forget();
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
