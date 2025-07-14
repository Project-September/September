using UnityEditor;
using UnityEngine;
using System.IO;

public class PhotonTrafficLoggerEditorWindow : EditorWindow
{
    private PhotonTrafficLoggerSettings _settings = new();
    private Vector2 _scrollPosition;
    
    [MenuItem("Tools/Photon Traffic Logger")]
    public static void ShowWindow()
    {
        GetWindow<PhotonTrafficLoggerEditorWindow>("Photon Traffic Logger");
    }
    
    private void OnEnable()
    {
        _settings.LoadFromPrefs();
    }
    
    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        
        GUILayout.Label("Photon Traffic Logger Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Enabled toggle
        EditorGUILayout.BeginHorizontal();
        _settings.Enabled = EditorGUILayout.Toggle("Enable Logger", _settings.Enabled);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Console output setting
        EditorGUILayout.BeginHorizontal();
        _settings.ConsoleOutput = EditorGUILayout.Toggle("Console Output", _settings.ConsoleOutput);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Log output path setting
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Log Output Path:");
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        string newPath = EditorGUILayout.TextField(_settings.LogOutputPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Log Output Directory", _settings.LogOutputPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                newPath = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        if (newPath != _settings.LogOutputPath)
        {
            _settings.LogOutputPath = newPath;
        }
        
        // Show resolved path
        EditorGUILayout.LabelField("Resolved Path:", _settings.LogOutputPath, EditorStyles.helpBox);
        
        EditorGUILayout.Space();
        
        // Log file limit setting
        EditorGUILayout.BeginHorizontal();
        _settings.LogFileLimit = EditorGUILayout.IntField("Log File Limit", _settings.LogFileLimit);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Status information
        GUILayout.Label("Status Information", EditorStyles.boldLabel);
        
        // Check if log directory exists
        bool logDirExists = Directory.Exists(_settings.LogOutputPath);
        string statusText = logDirExists ? "✓ Log directory exists" : "✗ Log directory does not exist";
        Color statusColor = logDirExists ? Color.green : Color.red;
        
        GUI.color = statusColor;
        EditorGUILayout.LabelField(statusText);
        GUI.color = Color.white;
        
        // Show current log files count
        if (logDirExists)
        {
            int logFileCount = Directory.GetFiles(_settings.LogOutputPath, "*.json").Length;
            EditorGUILayout.LabelField($"Current log files: {logFileCount}");
        }
        
        EditorGUILayout.Space();
        
        // Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create Log Directory"))
        {
            try
            {
                Directory.CreateDirectory(_settings.LogOutputPath);
                Debug.Log($"Created log directory: {_settings.LogOutputPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create log directory: {e.Message}");
            }
        }
        
        if (GUILayout.Button("Open Log Directory"))
        {
            if (Directory.Exists(_settings.LogOutputPath))
            {
                EditorUtility.RevealInFinder(_settings.LogOutputPath);
            }
            else
            {
                Debug.LogWarning("Log directory does not exist");
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All Logs"))
        {
            if (EditorUtility.DisplayDialog("Clear All Logs", 
                "Are you sure you want to delete all log files?", 
                "Yes", "No"))
            {
                ClearAllLogs();
            }
        }
        
        if (GUILayout.Button("Reset Settings"))
        {
            if (EditorUtility.DisplayDialog("Reset Settings", 
                "Are you sure you want to reset all settings to default?", 
                "Yes", "No"))
            {
                ResetSettings();
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndScrollView();
        
        // Save settings when GUI changes
        if (GUI.changed)
        {
            _settings.SaveToPrefs();
        }
    }
    
    private void ClearAllLogs()
    {
        try
        {
            if (Directory.Exists(_settings.LogOutputPath))
            {
                string[] logFiles = Directory.GetFiles(_settings.LogOutputPath, "*.json");
                foreach (string file in logFiles)
                {
                    File.Delete(file);
                }
                Debug.Log($"Cleared {logFiles.Length} log files");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to clear logs: {e.Message}");
        }
    }
    
    private void ResetSettings()
    {
        _settings = new PhotonTrafficLoggerSettings();
        _settings.SaveToPrefs();
        Debug.Log("Settings reset to default");
    }
}