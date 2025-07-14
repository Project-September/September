using UnityEngine;
using UnityEditor;

[System.Serializable]
public class PhotonTrafficLoggerSettings
{
    private const string CONSOLE_OUTPUT_KEY = "PhotonTrafficLogger.ConsoleOutput";
    private const string LOG_OUTPUT_PATH_KEY = "PhotonTrafficLogger.LogOutputPath";
    private const string LOG_FILE_LIMIT_KEY = "PhotonTrafficLogger.LogFileLimit";
    private const string ENABLED_KEY = "PhotonTrafficLogger.Enabled";
    
    [SerializeField] private bool consoleOutput = true;
    [SerializeField] private string logOutputPath = "";
    [SerializeField] private int logFileLimit = 10;
    [SerializeField] private bool enabled = false;
    
    public bool ConsoleOutput
    {
        get => consoleOutput;
        set
        {
            consoleOutput = value;
            EditorPrefs.SetBool(CONSOLE_OUTPUT_KEY, value);
        }
    }
    
    public string LogOutputPath
    {
        get => string.IsNullOrEmpty(logOutputPath) ? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "log") : logOutputPath;
        set
        {
            logOutputPath = value;
            EditorPrefs.SetString(LOG_OUTPUT_PATH_KEY, value);
        }
    }
    
    public int LogFileLimit
    {
        get => logFileLimit;
        set
        {
            logFileLimit = Mathf.Max(1, value);
            EditorPrefs.SetInt(LOG_FILE_LIMIT_KEY, logFileLimit);
        }
    }
    
    public bool Enabled
    {
        get => enabled;
        set
        {
            enabled = value;
            EditorPrefs.SetBool(ENABLED_KEY, value);
        }
    }
    
    public void LoadFromPrefs()
    {
        consoleOutput = EditorPrefs.GetBool(CONSOLE_OUTPUT_KEY, true);
        logOutputPath = EditorPrefs.GetString(LOG_OUTPUT_PATH_KEY, "");
        logFileLimit = EditorPrefs.GetInt(LOG_FILE_LIMIT_KEY, 10);
        enabled = EditorPrefs.GetBool(ENABLED_KEY, false);
    }
    
    public void SaveToPrefs()
    {
        EditorPrefs.SetBool(CONSOLE_OUTPUT_KEY, consoleOutput);
        EditorPrefs.SetString(LOG_OUTPUT_PATH_KEY, logOutputPath);
        EditorPrefs.SetInt(LOG_FILE_LIMIT_KEY, logFileLimit);
        EditorPrefs.SetBool(ENABLED_KEY, enabled);
    }
}