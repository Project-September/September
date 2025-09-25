#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SplineMirrorTool))]
public class SplineMirrorToolEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        var tool = (SplineMirrorTool)target;
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("Container"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("SourceSplineIndex"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Mode"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Reference"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Axis"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("WorldOffset"));

        if (tool.Mode == SplineMirrorTool.MirrorMode.Plane)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MirrorOrientation"));

        EditorGUILayout.Space(8);

        using (new EditorGUI.DisabledScope(!CanRun(tool, out string why)))
        {
            if (GUILayout.Button("Duplicate Now", GUILayout.Height(32)))
                Run(tool);
        }
        if (!CanRun(tool, out string reason))
            EditorGUILayout.HelpBox(reason, MessageType.Warning);

        serializedObject.ApplyModifiedProperties();
    }

    bool CanRun(SplineMirrorTool tool, out string reason)
    {
        var container = tool.GetContainerOrSelf();
        if (container == null) { reason = "SplineContainer が見つからない"; return false; }
        if (tool.SourceSplineIndex < 0 || tool.SourceSplineIndex >= container.Splines.Count)
        { reason = $"SourceSplineIndex が不正（0〜{container.Splines.Count - 1}）"; return false; }
        reason = null; return true;
    }

    void Run(SplineMirrorTool tool)
    {
        var container = tool.GetContainerOrSelf();
        Undo.RegisterCompleteObjectUndo(container, "Mirror Duplicate Spline");

        int newIndex = -1;

        if (tool.Mode == SplineMirrorTool.MirrorMode.Plane)
        {
            var p = tool.GetRefPoint();
            var n = tool.GetAxisDir();
            newIndex = tool.MirrorOrientation
                ? SplineMirrorOps.MirrorDuplicateAcrossPlaneSymmetric(container, tool.SourceSplineIndex, p, n, tool.WorldOffset)
                : SplineMirrorOps.MirrorDuplicateAcrossPlane(container, tool.SourceSplineIndex, p, n, tool.WorldOffset);
        }
        else
        {
            var p = tool.GetRefPoint();
            var d = tool.GetAxisDir();
            newIndex = SplineMirrorOps.HalfTurnDuplicateAroundLine(container, tool.SourceSplineIndex, p, d, tool.WorldOffset);
        }

        if (newIndex >= 0)
        {
            EditorUtility.SetDirty(container);
            SceneView.RepaintAll();
        }
    }
}
#endif
