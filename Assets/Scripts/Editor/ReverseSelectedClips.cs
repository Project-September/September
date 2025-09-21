#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AnimationClipReverserPro
{
    // 焼き込みのサンプル倍数。frameRate * RESAMPLE_MUL の密度で生成
    private const int RESAMPLE_MUL = 2;

    [MenuItem("Tools/Animation/Reverse Selected Clips (Strict Tangents)")]
    private static void ReverseSelected_Strict()
    {
        ProcessSelection(CreateReversedClip_Strict);
    }

    [MenuItem("Tools/Animation/Reverse Selected Clips (Resample Bake)")]
    private static void ReverseSelected_Resample()
    {
        ProcessSelection(CreateReversedClip_Resample);
    }

    [MenuItem("Assets/Create/Animation/Reverse This Clip (Strict Tangents)", true)]
    private static bool ValidateReverseThisClip() => Selection.activeObject is AnimationClip;

    [MenuItem("Assets/Create/Animation/Reverse This Clip (Strict Tangents)")]
    private static void ReverseThisClip_Strict()
    {
        var clip = Selection.activeObject as AnimationClip;
        ProcessOne(clip, CreateReversedClip_Strict);
    }

    [MenuItem("Assets/Create/Animation/Reverse This Clip (Resample Bake)")]
    private static void ReverseThisClip_Resample()
    {
        var clip = Selection.activeObject as AnimationClip;
        ProcessOne(clip, CreateReversedClip_Resample);
    }

    // 共通処理
    private static void ProcessSelection(Func<AnimationClip, AnimationClip> factory)
    {
        var clips = Selection.objects.OfType<AnimationClip>().Distinct().ToArray();
        if (clips.Length == 0)
        {
            EditorUtility.DisplayDialog("Reverse Clips", "AnimationClip を選択してください。", "OK");
            return;
        }

        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (var c in clips) CreateBeside(c, factory(c));
        }
        catch (Exception e) { Debug.LogException(e); }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void ProcessOne(AnimationClip clip, Func<AnimationClip, AnimationClip> factory)
    {
        if (clip == null) return;
        try
        {
            AssetDatabase.StartAssetEditing();
            CreateBeside(clip, factory(clip));
        }
        catch (Exception e) { Debug.LogException(e); }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void CreateBeside(AnimationClip src, AnimationClip dst)
    {
        var path = AssetDatabase.GetAssetPath(src);
        var dir = Path.GetDirectoryName(path);
        var name = Path.GetFileNameWithoutExtension(path);
        var dstPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(dir ?? "Assets", name + "_Reversed.anim"));
        AssetDatabase.CreateAsset(dst, dstPath);
        Debug.Log($"Created reversed clip: {dstPath}", dst);
    }

    // ——————————————————————————————————————————
    // 方式A: 厳密反転（タンジェント＆ウェイト入替＋符号反転）
    // ——————————————————————————————————————————
    public static AnimationClip CreateReversedClip_Strict(AnimationClip src)
    {
        if (!src) throw new ArgumentNullException(nameof(src));

        float len = Mathf.Max(src.length, 0f);
        var dst = NewClipLike(src, suffix: "_Reversed");

        // Float curves
        foreach (var b in AnimationUtility.GetCurveBindings(src))
        {
            var curve = AnimationUtility.GetEditorCurve(src, b);
            var rev = ReverseCurveStrict(curve, len);
            AnimationUtility.SetEditorCurve(dst, b, rev);
        }

        // Object reference curves (Spriteなど)
        foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(src))
        {
            var keys = AnimationUtility.GetObjectReferenceCurve(src, b);
            for (int i = 0; i < keys.Length; i++) keys[i].time = len - keys[i].time;
            Array.Sort(keys, (x, y) => x.time.CompareTo(y.time));
            AnimationUtility.SetObjectReferenceCurve(dst, b, keys);
        }

        // Events
        var evts = AnimationUtility.GetAnimationEvents(src);
        for (int i = 0; i < evts.Length; i++) evts[i].time = len - evts[i].time;
        Array.Sort(evts, (x, y) => x.time.CompareTo(y.time));
        AnimationUtility.SetAnimationEvents(dst, evts);

        EditorUtility.SetDirty(dst);
        return dst;
    }

    private static AnimationCurve ReverseCurveStrict(AnimationCurve src, float length)
    {
        if (src == null || src.keys == null || src.keys.Length == 0)
            return new AnimationCurve();

        var ks = src.keys;
        for (int i = 0; i < ks.Length; i++)
        {
            var k = ks[i];

            // 時間反転
            k.time = length - k.time;

            // in/out タンジェントは時間反転に合わせて入替＆符号反転
            var oldIn = k.inTangent;
            var oldOut = k.outTangent;
#if UNITY_2018_1_OR_NEWER
            var wm = k.weightedMode;
            var inW = k.inWeight;
            var outW = k.outWeight;
#endif
            k.inTangent = -oldOut;
            k.outTangent = -oldIn;

#if UNITY_2018_1_OR_NEWER
            // 加重タングェントの重みも入替
            k.inWeight = outW;
            k.outWeight = inW;
            k.weightedMode = wm; // モード自体はそのまま
#endif
            ks[i] = k;
        }

        Array.Sort(ks, (a, b) => a.time.CompareTo(b.time));

        var dst = new AnimationCurve(ks)
        {
            preWrapMode = src.preWrapMode,
            postWrapMode = src.postWrapMode
        };
        return dst;
    }

    // ——————————————————————————————————————————
    // 方式B: 再サンプル焼き（ガタつくなら最終兵器）
    // ——————————————————————————————————————————
    public static AnimationClip CreateReversedClip_Resample(AnimationClip src)
    {
        if (!src) throw new ArgumentNullException(nameof(src));

        float len = Mathf.Max(src.length, 0f);
        var dst = NewClipLike(src, suffix: "_Reversed");

        // Float curves: 均等サンプルで焼き直し
        foreach (var b in AnimationUtility.GetCurveBindings(src))
        {
            var curve = AnimationUtility.GetEditorCurve(src, b);
            var baked = ResampleReversed(curve, len, Mathf.Max(1, Mathf.RoundToInt(src.frameRate) * RESAMPLE_MUL));
            AnimationUtility.SetEditorCurve(dst, b, baked);
        }

        // Object reference: そのまま時間だけ反転
        foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(src))
        {
            var keys = AnimationUtility.GetObjectReferenceCurve(src, b);
            for (int i = 0; i < keys.Length; i++) keys[i].time = len - keys[i].time;
            Array.Sort(keys, (x, y) => x.time.CompareTo(y.time));
            AnimationUtility.SetObjectReferenceCurve(dst, b, keys);
        }

        // Events
        var evts = AnimationUtility.GetAnimationEvents(src);
        for (int i = 0; i < evts.Length; i++) evts[i].time = len - evts[i].time;
        Array.Sort(evts, (x, y) => x.time.CompareTo(y.time));
        AnimationUtility.SetAnimationEvents(dst, evts);

        EditorUtility.SetDirty(dst);
        return dst;
    }

    private static AnimationCurve ResampleReversed(AnimationCurve src, float length, int samples)
    {
        if (length <= 0f || src == null) return new AnimationCurve();

        var ks = new Keyframe[samples + 1];
        for (int i = 0; i <= samples; i++)
        {
            float tDst = i * (length / samples);          // 0..len
            float tSrc = length - tDst;                   // 反転元のサンプリング時刻
            float v = src.Evaluate(tSrc);

            var k = new Keyframe(tDst, v);
            // 線形で十分滑らか。必要なら後から SmoothTangents。
            ks[i] = k;
        }

        var dst = new AnimationCurve(ks)
        {
            preWrapMode = src.preWrapMode,
            postWrapMode = src.postWrapMode
        };

        // 端の見た目が気になるなら両端だけスムース化
        if (ks.Length >= 2)
        {
            AnimationUtility.SetKeyBroken(dst, 0, false);
            AnimationUtility.SetKeyBroken(dst, ks.Length - 1, false);
            dst.SmoothTangents(0, 0f);
            dst.SmoothTangents(ks.Length - 1, 0f);
        }

        return dst;
    }

    // 共通: クリップ設定のコピー（安全寄り簡略版）
    private static AnimationClip NewClipLike(AnimationClip src, string suffix)
    {
        var dst = new AnimationClip
        {
            name = src.name + suffix,
            frameRate = src.frameRate,
            legacy = src.legacy,
            localBounds = src.localBounds
        };

        // 主要な ClipSettings を SerializedObject 経由でコピー
        CopyClipSettings(src, dst);
        return dst;
    }

    private static void CopyClipSettings(AnimationClip src, AnimationClip dst)
    {
        var soSrc = new SerializedObject(src);
        var soDst = new SerializedObject(dst);

        CopyBool(soSrc, soDst, "m_AnimationClipSettings.m_LoopTime");
        CopyBool(soSrc, soDst, "m_AnimationClipSettings.m_LoopBlend");
        CopyFloat(soSrc, soDst, "m_AnimationClipSettings.m_StartTime");
        CopyFloat(soSrc, soDst, "m_AnimationClipSettings.m_StopTime");
        CopyBool(soSrc, soDst, "m_AnimationClipSettings.m_KeepOriginalPositionY");
        CopyBool(soSrc, soDst, "m_AnimationClipSettings.m_KeepOriginalOrientation");
        CopyBool(soSrc, soDst, "m_AnimationClipSettings.m_KeepOriginalPositionXZ");
        CopyBool(soSrc, soDst, "m_AnimationClipSettings.m_HeightFromFeet");
        CopyBool(soSrc, soDst, "m_AnimationClipSettings.m_Mirror");

        soDst.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CopyBool(SerializedObject s, SerializedObject d, string path)
    {
        var ps = s.FindProperty(path); var pd = d.FindProperty(path);
        if (ps != null && pd != null && ps.propertyType == SerializedPropertyType.Boolean)
        { pd.boolValue = ps.boolValue; }
    }
    private static void CopyFloat(SerializedObject s, SerializedObject d, string path)
    {
        var ps = s.FindProperty(path); var pd = d.FindProperty(path);
        if (ps != null && pd != null && ps.propertyType == SerializedPropertyType.Float)
        { pd.floatValue = ps.floatValue; }
    }
}
#endif
