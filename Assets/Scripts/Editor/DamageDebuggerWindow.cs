#if UNITY_EDITOR
using System.Reflection;
using Fusion;
using InGame.Health;
using UnityEditor;
using UnityEngine;

public class DamageDebuggerWindow : EditorWindow
{
    // 設定
    private GameObject _targetGo;
    private int _amount = 25;
    private bool _useSelection = true;

    // 反映用
    private IDamageable _cachedDamageable;

    // Ctrl/Cmd + Shift + D
    [MenuItem("Tools/September/Damage Debugger %#d")] 
    public static void Open()
    {
        GetWindow<DamageDebuggerWindow>("Damage Debugger");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4);

        _useSelection = EditorGUILayout.ToggleLeft("選択中のオブジェクトをターゲットにする", _useSelection);
        using (new EditorGUI.DisabledScope(_useSelection))
        {
            _targetGo = (GameObject)EditorGUILayout.ObjectField("ターゲット", _targetGo, typeof(GameObject), true);
        }

        _amount = EditorGUILayout.IntField("ダメージ/回復 量", Mathf.Max(1, _amount));

        var dmg = ResolveDamageable();
        using (new EditorGUI.DisabledScope(dmg == null))
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("現在HP", dmg != null ? GetHpText(dmg) : "-");

            EditorGUILayout.Space(4);
            if (GUILayout.Button("与ダメージ (Damage)"))
            {
                ApplyDamage(dmg, _amount);
            }
            if (GUILayout.Button("回復 (Heal)"))
            {
                ApplyHeal(dmg, _amount);
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("即死 (Kill)"))
            {
                ApplyDamage(dmg, 999999);
            }

            if (GUILayout.Button("最大HPまで回復 (Full Heal)"))
            {
                FullHeal(dmg);
            }

            if (GUILayout.Button("無敵切り替え (Toggle Invincible)"))
            {
                ToggleInvincible(dmg);
            }
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "・与ダメ/回復は IDamageable.TakeHit を通して適用します\n" +
            "・Kill/FullHeal/Invincible はリフレクションで内部フィールドを操作します（Editor実行時専用）\n" +
            "・選択オブジェクトに IDamageable が無ければ親から探索します",
            MessageType.Info);
    }

    private IDamageable ResolveDamageable()
    {
        GameObject go = _useSelection ? Selection.activeGameObject : _targetGo;

        if (go == null)
        {
            _cachedDamageable = null;
            return null;
        }

        // すでにキャッシュしていて、対象が変わっていなければ再利用
        if (_cachedDamageable is Component comp && comp != null && comp.gameObject == go)
            return _cachedDamageable;

        // 自身 → 親 で IDamageable 探索
        var dmg = go.GetComponentInParent(typeof(IDamageable)) as IDamageable;
        _cachedDamageable = dmg;
        return dmg;
    }

    private static void ApplyDamage(IDamageable dmg, int amount)
    {
        if (dmg == null) return;
        var hd = new HitData(HitActionType.Damage, amount, PlayerRef.None, dmg.OwnerPlayerRef);
        dmg.TakeHit(ref hd);
        MarkDirty(dmg);
    }

    private static void ApplyHeal(IDamageable dmg, int amount)
    {
        if (dmg == null) return;
        var hd = new HitData(HitActionType.Heal, amount, PlayerRef.None, dmg.OwnerPlayerRef);
        dmg.TakeHit(ref hd);
        MarkDirty(dmg);
    }

    private static void FullHeal(IDamageable dmg)
    {
        var comp = dmg as Component;
        if (!TryGetPrivateField<int>(comp, "_maxHealth", out var max)) return;
        SetPrivateField(comp, "_currentHealth", max);
        MarkDirty(dmg);
    }

    private static void ToggleInvincible(IDamageable dmg)
    {
        var comp = dmg as Component;
        if (!TryGetPrivateField<bool>(comp, "_isInvincible", out var inv)) return;
        SetPrivateField(comp, "_isInvincible", !inv);
        MarkDirty(dmg);
    }

    private static string GetHpText(IDamageable dmg)
    {
        var comp = dmg as Component;
        int cur = TryGetPrivateField<int>(comp, "_currentHealth", out var c) ? c : -1;
        int max = TryGetPrivateField<int>(comp, "_maxHealth", out var m) ? m : -1;
        return (cur >= 0 && max >= 0) ? $"{cur} / {max}" : "(参照不可)";
    }

    private static void MarkDirty(IDamageable dmg)
    {
        var comp = dmg as Component;
        if (comp != null) EditorUtility.SetDirty(comp);
    }

    // ------- リフレクション 汎用 -------
    private static bool TryGetPrivateField<T>(Component comp, string fieldName, out T value)
    {
        value = default;
        if (comp == null) return false;
        var tp = comp.GetType();
        var fi = tp.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (fi == null || !typeof(T).IsAssignableFrom(fi.FieldType)) return false;
        value = (T)fi.GetValue(comp);
        return true;
    }

    private static void SetPrivateField<T>(Component comp, string fieldName, T v)
    {
        if (comp == null) return;
        var tp = comp.GetType();
        var fi = tp.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (fi == null) return;
        fi.SetValue(comp, v);
    }
}
#endif
