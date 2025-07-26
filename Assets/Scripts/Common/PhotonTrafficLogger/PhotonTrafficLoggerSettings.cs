using UnityEngine;

namespace PhotonTrafficLogger
{
    [System.Serializable]
    public class PhotonTrafficLoggerSettings
{
    private const string ConsoleOutputKey = "PhotonTrafficLogger.ConsoleOutput";
    private const string LOGOutputPathKey = "PhotonTrafficLogger.LogOutputPath";
    private const string LOGFileLimitKey = "PhotonTrafficLogger.LogFileLimit";
    private const string EnabledKey = "PhotonTrafficLogger.Enabled";

    [SerializeField] private bool consoleOutput;
    [SerializeField] private string logOutputPath = "";
    [SerializeField] private int logFileLimit = 10;
    [SerializeField] private bool enabled;

    public bool ConsoleOutput
    {
        get => consoleOutput;
        set => consoleOutput = value;
    }

    public string LogOutputPath
    {
        get => string.IsNullOrEmpty(logOutputPath)
            ? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "photon_logs")
            : logOutputPath;
        set => logOutputPath = value;
    }

    public int LogFileLimit
    {
        get => logFileLimit;
        set => logFileLimit = Mathf.Max(1, value);
    }

    public bool Enabled
    {
        get => enabled;
        set => enabled = value;
    }

    public void LoadFromPrefs()
    {
        consoleOutput = PlayerPrefs.GetInt(ConsoleOutputKey, 1) == 1;
        logOutputPath = PlayerPrefs.GetString(LOGOutputPathKey, "");
        logFileLimit = PlayerPrefs.GetInt(LOGFileLimitKey, 10);
        enabled = PlayerPrefs.GetInt(EnabledKey, 0) == 1;

        Debug.Log(
            $"[PhotonTrafficLoggerSettings] LoadFromPrefs - ConsoleOutput: {consoleOutput}, LogOutputPath: '{logOutputPath}', LogFileLimit: {logFileLimit}, Enabled: {enabled}");
    }

    public void SaveToPrefs()
    {
        PlayerPrefs.SetInt(ConsoleOutputKey, consoleOutput ? 1 : 0);
        PlayerPrefs.SetString(LOGOutputPathKey, logOutputPath);
        PlayerPrefs.SetInt(LOGFileLimitKey, logFileLimit);
        PlayerPrefs.SetInt(EnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log(
            $"[PhotonTrafficLoggerSettings] SaveToPrefs - ConsoleOutput: {consoleOutput}, LogOutputPath: '{logOutputPath}', LogFileLimit: {logFileLimit}, Enabled: {enabled}");
    }
    }
}