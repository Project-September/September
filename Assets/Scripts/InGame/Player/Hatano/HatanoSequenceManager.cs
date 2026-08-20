using Fusion;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace InGame.Player.Hatano
{
    public class HatanoSequenceManager : NetworkBehaviour
    {
        [SerializeField] private PlayableDirector _director;
        [SerializeField] private TimelineAsset _startTimeline;
        [SerializeField] private TimelineAsset _endTimeline;

        [Header("武器（アニメーションFBX）")]
        [SerializeField] private GameObject _rocketAnimFBX;
        [SerializeField] private GameObject[] _doubleAnimFBX;
        [Header("武器（Prefab）")]
        [SerializeField] private GameObject _rocketPrefab;
        [SerializeField] private GameObject _laserPrefab;
        [SerializeField] private GameObject[] _doublePrefabs;
        
        private HatanoAbilityStatusManagement _hatanoAbilityStatusManagement;

        public override void Spawned()
        {
            _hatanoAbilityStatusManagement = GetComponent<HatanoAbilityStatusManagement>();
        }

        public bool IsSequencePlaying()
        {
            return _director.state == PlayState.Playing;
        }
        
        [Rpc]
        public void RPC_SetEndTimeline()
        {
            _director.playableAsset = _endTimeline;
            _director.Play();
        }

        [Rpc]
        public void RPC_SetStartTimeline()
        {
            _director.playableAsset = _startTimeline;
            
            _rocketPrefab.SetActive(true);
            if(_hatanoAbilityStatusManagement?.AbilityStatus == HatanoAbilityStatus.LaserGun) _laserPrefab.SetActive(true);
            else foreach(var obj in _doublePrefabs) obj.SetActive(true);
        }

        [Rpc]
        public void RPC_WeaponFBXDisplayToggle(bool flag)
        {
            _rocketAnimFBX.SetActive(flag);
            foreach(var obj in _doubleAnimFBX) obj.SetActive(flag);
        }
        
        [Rpc]
        public void RPC_WeaponPrefabHidden()
        {
            _rocketPrefab.SetActive(false);
            _laserPrefab.SetActive(false);
            foreach(var obj in _doublePrefabs) obj.SetActive(false);
        }
    }
}
