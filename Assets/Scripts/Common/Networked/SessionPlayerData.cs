using Fusion;

namespace September.Common
{
    public struct SessionPlayerData : INetworkStruct
    {
        public NetworkString<_16> PureNickName => _nickName;

        public string DisplayNickName
        {
            get
            {
                if (_nickNameOrder == 0)
                {
                    return _nickName.Value;
                }
                else
                {
                    return $"{_nickName.Value}_{_nickNameOrder}";
                }
            }
        }
        readonly NetworkString<_16> _nickName;
        readonly int _nickNameOrder;
        public CharacterType CharacterType;
        public BuildType BuildType;
        public NetworkBool IsOgre;
        public int Score;
        [Networked, Capacity(7)] public NetworkDictionary<PlayerRef, int> StunData => default;

        public int DamageReceived;
        public int DamageDealt;
        public int OgreCount;
        public int TotalInteractCount;

        public SessionPlayerData(NetworkString<_16> nickName, int nickNameOrder)
        {
            _nickName = nickName;
            _nickNameOrder = nickNameOrder;
            CharacterType = CharacterType.OkabeWright;
            BuildType = BuildType.AttackPower;
            IsOgre = false;
            Score = 0;

            DamageDealt = 0;
            DamageReceived = 0;
            OgreCount = 0;
            TotalInteractCount = 0;
        }
    }
}