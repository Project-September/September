using UnityEngine;

namespace September.NewResult
{
    public class PlayerDetailsView : MonoBehaviour, IPlayerDetailsView
    {
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private PlayerDetailsEntryView _playerDetailsEntryViewPrefab;

        public void Setup(PlayerDetailsModel[] playerDetails)
        {
            foreach (Transform child in _contentRoot)
            {
                Destroy(child.gameObject);
            }
            
            foreach (var model in playerDetails)
            {
                var view = Instantiate(_playerDetailsEntryViewPrefab, _contentRoot);
                view.Set(model);
            }
        }
    }
	
    public struct PlayerDetailsModel
    {
        public Sprite CharacterSprite;
        public string PlayerName;
        public int PlayerScore;
        public int PlayerDamageDealt;
        public int PlayerDamageReceived;
        public int PlayerOgreCount;
        public int PlayerExhibitsInteractCount;
        public int Rank;
        public bool IsOgre;
    }
}