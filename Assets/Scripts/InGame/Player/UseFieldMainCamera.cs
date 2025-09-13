using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public class UseFieldMainCamera : MonoBehaviour
{
    [SerializeField] private string _fieldSceneName = "Field";

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == _fieldSceneName)
            TrySwitch();
    }

    private void TrySwitch()
    {
        var field = SceneManager.GetSceneByName(_fieldSceneName);
        if (!field.IsValid() || !field.isLoaded) return;

        var fieldCam = FindRootCamera(field);
        if (fieldCam == null)
        {
            Debug.LogWarning($"[{nameof(UseFieldMainCamera)}] \"{_fieldSceneName}\" のルート直下に Camera が見つかりませんでした。");
            return;
        }

        // いま有効なカメラは全部オフ
        var enabledCams = Camera.allCameras;
        foreach (var cam in enabledCams)
        {
            if (cam == null) continue;
            if (cam == fieldCam) continue;
            var go = cam.gameObject;
            if (go.activeSelf) go.SetActive(false);
        }

        // FieldのカメラだけON（子にしない前提）
        if (!fieldCam.gameObject.activeSelf)
            fieldCam.gameObject.SetActive(true);

    }

    private static Camera FindRootCamera(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        foreach (var gameObject in roots)
        {
            if (gameObject.TryGetComponent<Camera>(out var cam))
                return cam;
        }
        return null;
    }
}