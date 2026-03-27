using System.Collections.Generic;
using InGame.Interact;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InGame.Exhibit
{
    /// <summary>
    /// 全展示物を保持するクラス（仮）
    /// </summary>
    public class ExhibitRegistry : SingletonMonoBehaviour<ExhibitRegistry>
    {
        [SerializeField] private List<InteractableBase> _items = new();
        
        public IReadOnlyList<InteractableBase> Items => _items;

        public void Add(InteractableBase effect) => _items.Add(effect);
        public void Remove(InteractableBase effect) => _items.Remove(effect);

        #if UNITY_EDITOR
        [ContextMenu(nameof(SearchAll))]
        public void SearchAll()
        {
            Undo.RecordObject(this, "Search All");
            _items.Clear();
            _items.AddRange(FindObjectsByType<InteractableBase>(FindObjectsSortMode.None));
        }
        #endif
    }
}