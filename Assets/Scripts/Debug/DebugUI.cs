using Fusion;
using September.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugUI : MonoBehaviour
{
    [SerializeField] private GameObject _debugPanel;
    [SerializeField] private KeyCode _toggleKey = KeyCode.F12;
    
    private bool _isDebugUIVisible;
    private CursorLockMode _previousCursorLockMode;
    private bool _previousCursorVisible;
    private static DebugUI _instance;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
        
        SceneManager.sceneLoaded += OnEnterIngameScene;
    }
    
    private void OnEnterIngameScene(Scene scene, LoadSceneMode mode)
    {
        if (SceneManager.GetActiveScene().name != "InGameMock")
        {
            return;
        }
        var statics = GameObject.Find("FusionStatsRenderPanel(Clone)");
        if (statics != null)
        {
            statics.GetComponent<FusionBasicBillboard>().enabled = false;
        }
    }

    private void Initialize()
    {
        if (_debugPanel != null)
        {
            _debugPanel.SetActive(false);
        }
        _isDebugUIVisible = false;
        _debugPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            ToggleDebugUI();
        }
    }
    
    private void ToggleDebugUI()
    {
        _isDebugUIVisible = !_isDebugUIVisible;
        
        if (_debugPanel != null)
        {
            _debugPanel.SetActive(_isDebugUIVisible);
        }
        
        if (_isDebugUIVisible)
        {
            ShowDebugUI();
        }
        else
        {
            HideDebugUI();
        }
    }
    
    private void ShowDebugUI()
    {
        _previousCursorLockMode = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;

        CursorStateManager.ShowCursor();

        if (_debugPanel != null)
        {
            _debugPanel.transform.SetAsLastSibling();
        }
    }
    
    private void HideDebugUI()
    {
        CursorStateManager.HideCursor();
    }
}
