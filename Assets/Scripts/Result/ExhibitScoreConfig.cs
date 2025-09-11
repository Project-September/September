using System;
using System.Collections.Generic;
using Result;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Score",fileName = "Score")]
public class ExhibitScoreConfig : ScriptableObject
{
    [SerializeField] private List<ExhibitScoreEntry> _entries = new();
        
    private Dictionary<ExhibitType, int> _lookup;

    public IReadOnlyList<ExhibitScoreEntry> Entries => _entries;

    public int GetPoint(ExhibitType type)
    {
        if (_lookup != null) 
            return _lookup.GetValueOrDefault(type, 0);
            
        _lookup = new Dictionary<ExhibitType, int>();
        foreach (ExhibitScoreEntry e in _entries)
            _lookup[e.Type] = e.Points;
        return _lookup.GetValueOrDefault(type, 0);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncWithEnum();
        _entries.Sort((a, b) => a.Type.CompareTo(b.Type));
        EditorUtility.SetDirty(this);
    }

    [ContextMenu("Sync From Enum")]
    public void SyncWithEnum()
    {
        var seen = new HashSet<ExhibitType>();
        // 既存の正規化：重複除去・Noneは任意でスキップ
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            var t = _entries[i].Type;
            if (t == ExhibitType.None || !seen.Add(t))
                _entries.RemoveAt(i);
        }
            
        foreach (ExhibitType t in Enum.GetValues(typeof(ExhibitType)))
        {
            if (t == ExhibitType.None) 
                continue;
            if (!seen.Contains(t))
            {
                _entries.Add(new ExhibitScoreEntry
                {
                    Type = t,
                    Points = 100 
                });
            }
        }
            
        _lookup = null;
    }
#endif
}