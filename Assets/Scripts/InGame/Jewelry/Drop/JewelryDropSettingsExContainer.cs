using UnityEngine;

namespace September.InGame.Jewelry.Drop
{
    [CreateAssetMenu(fileName = "JewelryDropSettings", menuName = "ScriptableObjects/Drop Settings Ex")]
    public class JewelryDropSettingsExContainer : ScriptableObject
    {
        [SerializeField] private JewelryDropSettingsEx _settings;

        public JewelryDropSettingsEx Settings => _settings;
    }
}
