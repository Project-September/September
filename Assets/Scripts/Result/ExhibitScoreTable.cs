using System;

namespace Result
{
    public enum ExhibitType : byte
    {
        None,
        Ptr,
        TRex,
        Art,
        AirPlane,
        FlagealCamouflage,
        Tutankhamun,
        LondonTelephone,
        Car,
        Moai,
        SateliteCanon,
        Instrument,
    }

    [Serializable]
    public struct ExhibitScoreEntry
    {
        public ExhibitType Type;
        public int Points;
    }
}