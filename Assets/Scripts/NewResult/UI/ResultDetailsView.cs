using UnityEngine;
using UnityEngine.Serialization;

namespace NewResult.UI
{
    public interface IResultDetailsView
    {
        void Setup(PlayerDetailsModel[] playerDetails);
    }
    
    public class ResultDetailsView : MonoBehaviour, IResultDetailsView
    {
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private PlayerDetailsView _playerDetailsViewPrefab;

        public void Setup(PlayerDetailsModel[] playerDetails)
        {
            foreach (Transform child in _contentRoot)
            {
                Destroy(child.gameObject);
            }
            
            foreach (var model in playerDetails)
            {
                var view = Instantiate(_playerDetailsViewPrefab, _contentRoot);
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