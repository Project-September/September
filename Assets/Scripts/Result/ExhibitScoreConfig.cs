using System.Collections.Generic;
using Result;
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
}