#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>Menu画面にシーンを移動するボタンを追加</summary>
public static class SceneNavigation
{
    [MenuItem("Scene/TitleScene")]
    public static void Scene0()
    {
        EditorSceneManager.SaveOpenScenes();
        OpenScene(0);
    }

    [MenuItem("Scene/Map_Museum")]
    public static void Museum()
    {
        EditorSceneManager.SaveOpenScenes();
        OpenScene(1);
        OpenScene(5, OpenSceneMode.Additive);
    }

    [MenuItem("Scene/Map_Pirate")]
    public static void Pirate()
    {
        EditorSceneManager.SaveOpenScenes();
        OpenScene(9);
    }

    [MenuItem("Scene/ResultScene")]
    public static void Result()
    {
        EditorSceneManager.SaveOpenScenes();
        OpenScene(7);
    }

    [MenuItem("Scene/DevStartInScene")]
    public static void Scene03()
    {
        EditorSceneManager.SaveOpenScenes();
        OpenScene(3);
    }

    [MenuItem("Scene/Lobby")]
    public static void Scene04()
    {
        EditorSceneManager.SaveOpenScenes();
        OpenScene(4);
    }

    private static void OpenScene(int sceneIndex, OpenSceneMode openMode = OpenSceneMode.Single)
    {
        string scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
        if (!string.IsNullOrEmpty(scenePath))
        {
            EditorSceneManager.OpenScene(scenePath, openMode);
        }
    }
}
#endif
