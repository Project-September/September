using Fusion;
using September.Common;
using UnityEngine;

namespace Ingame.Tanihira
{
    public class FriendOrder : NetworkBehaviour, IAfterTick
    {
        [SerializeField] private GameObject _friendOwner;
        private FormationManager _formationManager;
        private TanihiraCursor _cursor;
        private FriendPlayerDetector _detector;
        private bool _isOrdering;
        private NetworkButtons PreviousButtons { get; set; }

        public override void Spawned()
        {
            _formationManager = GetComponent<FormationManager>();
            _cursor = GetComponent<TanihiraCursor>();
            _detector = GetComponentInChildren<FriendPlayerDetector>();
        }

        public override void FixedUpdateNetwork()
        {
            if (!GetInput<PlayerInput>(out var input)) return;
            
            if (input.Buttons.WasPressed(PreviousButtons, PlayerButtons.Ability1))
            {
                OrderReturnFriend();
            }
            
        }

        public void ExecuteOrderMoveFriend()
        {
            if (_formationManager == null || _cursor == null)
                return;

            foreach (var friend in _formationManager.FriendsList)
            {
                friend.SetDestination(_cursor.MoveTargetTransform);
                friend.ChangeState(FriendState.Move);
            }
            
            //索敵の中心をペンギンに変更
            _detector.ChangeDetectionCenter(_formationManager.GetBossFriend().transform, false, _cursor.MoveTargetTransform);
        }

        public void OrderReturnFriend()
        {
            if (_formationManager == null)
                return;

            foreach (var friend in _formationManager.FriendsList)
            {
                friend.SetDestination(_friendOwner.transform);
                friend.ChangeState(FriendState.Move);
                //隊列の整理をする
                _formationManager.SortFormation();
            }
            
            //索敵の中心をプレイヤーに変更
            _detector.ChangeDetectionCenter(this.transform, true);
        }
        
        public void AfterTick()
        {
            PreviousButtons = GetInput<PlayerInput>().GetValueOrDefault().Buttons;
        }
    }
}