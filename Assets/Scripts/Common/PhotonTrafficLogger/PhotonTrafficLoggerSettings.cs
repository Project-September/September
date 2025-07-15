using UnityEngine;

[System.Serializable]
public class PhotonTrafficLoggerSettings
{
    private const string ConsoleOutputKey = "PhotonTrafficLogger.ConsoleOutput";
    private const string LOGOutputPathKey = "PhotonTrafficLogger.LogOutputPath";
    private const string LOGFileLimitKey = "PhotonTrafficLogger.LogFileLimit";
    private const string EnabledKey = "PhotonTrafficLogger.Enabled";
    
    [SerializeField] private bool consoleOutput = true;
    [SerializeField] private string logOutputPath = "";
    [SerializeField] private int logFileLimit = 10;
    [SerializeField] private bool enabled;

    public bool ConsoleOutput
    {
        get => consoleOutput;
        set
        {
            consoleOutput = value;
            PlayerPrefs.SetInt(ConsoleOutputKey, value ? 1 : 0);
        }
    }
    
    public string LogOutputPath
    {
        get => string.IsNullOrEmpty(logOutputPath) ? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "photon_logs") : logOutputPath;
        set
        {
            logOutputPath = value;
            PlayerPrefs.SetString(LOGOutputPathKey, value);
        }
    }
    
    public int LogFileLimit
    {
        get => logFileLimit;
        set
        {
            logFileLimit = Mathf.Max(1, value);
            PlayerPrefs.SetInt(LOGFileLimitKey, logFileLimit);
        }
    }
    
    public bool Enabled
    {
        get => enabled;
        set
        {
            enabled = value;
            PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
        }
    }
    
    public void LoadFromPrefs()
    {
        consoleOutput = PlayerPrefs.GetInt(ConsoleOutputKey, 1) == 1;
        logOutputPath = PlayerPrefs.GetString(LOGOutputPathKey, "");
        logFileLimit = PlayerPrefs.GetInt(LOGFileLimitKey, 10);
        enabled = PlayerPrefs.GetInt(EnabledKey, 0) == 1;
    }
    
    public void SaveToPrefs()
    {
        PlayerPrefs.SetInt(ConsoleOutputKey, consoleOutput ? 1 : 0);
        PlayerPrefs.SetString(LOGOutputPathKey, logOutputPath);
        PlayerPrefs.SetInt(LOGFileLimitKey, logFileLimit);
        PlayerPrefs.SetInt(EnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}
