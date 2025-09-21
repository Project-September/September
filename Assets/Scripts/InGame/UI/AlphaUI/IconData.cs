using September.Common;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "September/UI/IconData", menuName = "Icon Data")]
public class IconData : ScriptableObject
{
    [SerializeField] private SerializableDictionary<CharacterType,Sprite> _iconDictionary = new();
    
    public SerializableDictionary<CharacterType,Sprite> IconDictionary => _iconDictionary;
}
