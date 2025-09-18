using System.Collections.Generic;
using Result;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Score",fileName = "Score")]
public class ExhibitScoreConfig : ScriptableObject
{
    [SerializeField] private List<ExhibitScoreEntry> _entries = new();
        
    private Dictionary<ExhibitType, int> _interactLookup;
    private Dictionary<ExhibitType, int> _destroyLookup;

    public IReadOnlyList<ExhibitScoreEntry> Entries => _entries;

    // インタラクト時のポイント
    public int GetPoint(ExhibitType type)
    {
        if (_interactLookup != null) 
            return _interactLookup.GetValueOrDefault(type, 0);
            
        _interactLookup = new Dictionary<ExhibitType, int>();
        foreach (ExhibitScoreEntry e in _entries)
            _interactLookup[e.Type] = e.Points;
        return _interactLookup.GetValueOrDefault(type, 0);
    }
    
    // ハルク破壊時
    public int GetDestroyPoint(ExhibitType type)
    {
        if (_destroyLookup == null)
        {
            _destroyLookup = new Dictionary<ExhibitType, int>();
            foreach (ExhibitScoreEntry e in _entries)
            {
                _destroyLookup[e.Type] = e.DestroyPoints;
            }
        }

        return _destroyLookup.GetValueOrDefault(type, 0);
    }
}