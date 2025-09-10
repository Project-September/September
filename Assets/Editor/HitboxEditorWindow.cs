using UnityEngine;
using UnityEditor;

public class HitboxEditorWindow : EditorWindow
{
    int currentFrame = 0;

    [MenuItem("Tools/Hitbox Editor")]
    public static void OpenWindow()
    {
        GetWindow<HitboxEditorWindow>("Hitbox Editor");
    }

    void OnGUI()
    {
        GUILayout.Label("Hitbox Editor", EditorStyles.boldLabel);

        // Current Frame
        currentFrame = EditorGUILayout.IntField("Current Frame", currentFrame);

        if (GUILayout.Button("Previous Frame"))
            currentFrame = Mathf.Max(0, currentFrame - 1);

        if (GUILayout.Button("Next Frame"))
            currentFrame++;

        // Scene���HitboxVisualizer�ɓ���
        HitboxVisualizer[] visualizers = FindObjectsOfType<HitboxVisualizer>();
        foreach (var viz in visualizers)
        {
            viz.currentFrame = currentFrame;
            EditorUtility.SetDirty(viz);
        }

        // HitboxFrameData�̕ҏWUI�i�ŏ���Visualizer���Q�Ɓj
        HitboxVisualizer firstViz = FindObjectOfType<HitboxVisualizer>();
        if (firstViz != null && firstViz.hitboxData != null && firstViz.hitboxData.frames.Length > 0)
        {
            int index = Mathf.Clamp(currentFrame, 0, firstViz.hitboxData.frames.Length - 1);
            var frameData = firstViz.hitboxData.frames[index];

            // �t���[���̒l��ҏW
            frameData.hitboxPos = EditorGUILayout.Vector3Field("HitboxPos", frameData.hitboxPos);
            frameData.hitboxSize = EditorGUILayout.Vector3Field("HitboxSize", frameData.hitboxSize);
            frameData.damage = EditorGUILayout.IntField("Damage", frameData.damage);
            frameData.rootOffset = EditorGUILayout.Vector3Field("RootOffset", frameData.rootOffset);

            EditorUtility.SetDirty(firstViz.hitboxData); // ScriptableObject�X�V��ʒm
        }
    }
}
