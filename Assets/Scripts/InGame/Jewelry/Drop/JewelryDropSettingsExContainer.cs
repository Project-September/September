using UnityEngine;

namespace September.InGame.Jewelry.Drop
{
    [CreateAssetMenu(fileName = "JewelryDropSettings", menuName = "ScriptableObjects/Drop Settings Ex")]
    public class JewelryDropSettingsExContainer : ScriptableObject
    {
        [SerializeField] private JewelryDropSettings _settings;

        public JewelryDropSettings Settings => _settings;
    }
}
