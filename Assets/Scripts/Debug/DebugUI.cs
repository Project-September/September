using UnityEngine;
using UnityEngine.UI;

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
    }

    private void Initialize()
    {
        if (_debugPanel != null)
        {
            _debugPanel.SetActive(false);
        }
        _isDebugUIVisible = false;
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
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        if (_debugPanel != null)
        {
            _debugPanel.transform.SetAsLastSibling();
        }
    }
    
    private void HideDebugUI()
    {
        Cursor.lockState = _previousCursorLockMode;
        Cursor.visible = _previousCursorVisible;
    }
}
