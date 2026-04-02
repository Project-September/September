using UnityEditor;
using UnityEngine;

public class PackageExporter : EditorWindow {

    [MenuItem("September/Export")] // ヘッダメニュー名/ヘッダ以下のメニュー名
    private static void ShowWindow() {
        var window = GetWindow<PackageExporter>();
        window.titleContent = new GUIContent("ExportPackage"); 
        window.Show();
    }
}