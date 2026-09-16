using UnityEngine;

namespace CRISound
{
    /// <summary> キューのシート名とキュー名を持つ型 </summary>
    public readonly struct CueData
    {
        public string Sheet { get; }
        public string Name { get; }

        public CueData(string sheet, string name)
        {
            Sheet = sheet;
            Name = name;
        }
    }

    /// <summary>
    /// キューを指定する場合はここから
    /// 現在入っている音のみ(随時更新)
    /// </summary>
    public static class SoundCues
    {
        private const string SheetName = "ALLCue";

        public static class BGM
        {
            public static readonly CueData Ingame_01 = new CueData(SheetName, "BGM_Ingame_01");
            //public static readonly CueData Ingame_last_01 = new CueData(SheetName, "BGM_Ingame_last_01"); // 未
            public static readonly CueData Okb_01 = new CueData(SheetName, "BGM_Okb_01");
            public static readonly CueData Haruku_01 = new CueData(SheetName, "BGM_Haruku_01");
            public static readonly CueData Tanihira_01 = new CueData(SheetName, "BGM_Tanihira_01"); // 未
            public static readonly CueData Koinuma_01 = new CueData(SheetName, "BGM_Koinuma_01");
            public static readonly CueData Select_01_Loop = new CueData(SheetName, "BGM_Select_01");
            public static readonly CueData Result_01_Loop = new CueData(SheetName, "BGM_Result_01");
            public static readonly CueData Title_01 = new CueData(SheetName, "BGM_Title_01"); // 未
        }

        public static class SE
        {
            public static readonly CueData Player_Revive = new CueData(SheetName, "SE_Player_Revive"); // 未
            public static readonly CueData Player_Punch_Swing = new CueData(SheetName, "SE_Player_Punch_Swing");
            public static readonly CueData Player_Punch_Hit = new CueData(SheetName, "SE_Player_Punch_Hit");
            public static readonly CueData Player_Knockout = new CueData(SheetName, "SE_Player_Knockout");
            public static readonly CueData Player_Footstep_Generic = new CueData(SheetName, "SE_Player_Footstep_Generic");
            public static readonly CueData OKB_Footstep = new CueData(SheetName, "SE_OKB_Footstep");
            public static readonly CueData Hulk_Footstep = new CueData(SheetName, "SE_Hulk_Footstep");
            public static readonly CueData Hulk_Yell = new CueData(SheetName, "SE_Hulk_Yell");
            public static readonly CueData Hulk_Break = new CueData(SheetName, "SE_Hulk_Break"); // 未
            public static readonly CueData Tanihira_Footstep = new CueData(SheetName, "SE_Tanihira_Footstep"); // 未 仮でGenericの足音を導入
            public static readonly CueData Penguin_Footstep = new CueData(SheetName, "SE_Penguin_Footstep"); // 未
            public static readonly CueData Penguin_Slide = new CueData(SheetName, "SE_Penguin_Slide"); // 未
            public static readonly CueData Penguin_Attack = new CueData(SheetName, "SE_Penguin_Attack"); // 未
            public static readonly CueData Tanihira_FriendCall = new CueData(SheetName, "SE_Tanihira_FriendCall"); // 未
            public static readonly CueData Sarutobi_Appear = new CueData(SheetName, "SE_Sarutobi_Appear"); // 未
            public static readonly CueData Sarutobi_Bomb = new CueData(SheetName, "SE_Sarutobi_Bomb"); // 未
            public static readonly CueData Sarutobi_Grapple_Prepare = new CueData(SheetName, "SE_Sarutobi_Grapple_Prepare");
            public static readonly CueData Sarutobi_Grapple_Land = new CueData(SheetName, "SE_Sarutobi_Grapple_Land");
            public static readonly CueData Sarutobi_Grapple_Move = new CueData(SheetName, "SE_Sarutobi_Grapple_Move");
            public static readonly CueData UI_CooldownReady = new CueData(SheetName, "SE_UI_CooldownReady"); // 未
            public static readonly CueData UI_ModeChange = new CueData(SheetName, "SE_UI_ModeChange"); // 未
            public static readonly CueData UI_ModeChanged = new CueData(SheetName, "SE_UI_ModeChanged");
            public static readonly CueData Tutankhamen_Interact = new CueData(SheetName, "SE_Tutankhamen_Interact");
            public static readonly CueData Exhibit_Revive = new CueData(SheetName, "SE_Exhibit_Revive");
            public static readonly CueData Muramasa_Interact = new CueData(SheetName, "SE_Muramasa_Interact");
            public static readonly CueData Muramasa_Attack = new CueData(SheetName, "SE_Muramasa_Attack");
            public static readonly CueData OpticalCamo_Activate = new CueData(SheetName, "SE_OpticalCamo_Activate");
            public static readonly CueData LondonPhone_Interact = new CueData(SheetName, "SE_LondonPhone_Interact");
            public static readonly CueData LondonPhone_EffectLoop = new CueData(SheetName, "SE_LondonPhone_EffectLoop"); // 未
            public static readonly CueData StrikeBarricade_Interact = new CueData(SheetName, "SE_StrikeBarricade_Interact");
            public static readonly CueData BeltBall_Shoot = new CueData(SheetName, "SE_BeltBall_Shoot");
            public static readonly CueData BeltBall_Roll = new CueData(SheetName, "SE_BeltBall_Roll");
            public static readonly CueData SatelliteCannon_Interact = new CueData(SheetName, "SE_SatelliteCannon_Interact"); // ボイス
            public static readonly CueData SatelliteCannon_LockOn = new CueData(SheetName, "SE_SatelliteCannon_LockOn");
            public static readonly CueData SatelliteCannon_LaserFire = new CueData(SheetName, "SE_SatelliteCannon_LaserFire");
            //public static readonly CueData SatelliteCannon_LaserCharge = new CueData(SheetName, "SE_SatelliteCannon_LaserCharge");
            //public static readonly CueData SatelliteCannon_ChargeGlow = new CueData(SheetName, "SE_SatelliteCannon_ChargeGlow");
            //public static readonly CueData SatelliteCannon_EnergyBeam = new CueData(SheetName, "SE_SatelliteCannon_EnergyBeam"); // 未
            public static readonly CueData ZeroFighter_Interact = new CueData(SheetName, "SE_ZeroFighter_Interact");
            public static readonly CueData ZeroFighter_PropellerLoop = new CueData(SheetName, "SE_ZeroFighter_PropellerLoop");
            public static readonly CueData ZeroFighter_TakeoffGunFire = new CueData(SheetName, "SE_ZeroFighter_TakeoffGunFire");
            public static readonly CueData ZeroFighter_CrashExplosion = new CueData(SheetName, "SE_ZeroFighter_CrashExplosion");
            public static readonly CueData Pteranodon_AttackFire = new CueData(SheetName, "SE_Pteranodon_AttackFire");
            public static readonly CueData Pteranodon_RideStart = new CueData(SheetName, "SE_Pteranodon_RideStart");
            public static readonly CueData Pteranodon_FlapLoop = new CueData(SheetName, "SE_Pteranodon_FlapLoop");
            public static readonly CueData ZeroFighter_RideAction = new CueData(SheetName, "SE_ZeroFighter_RideAction"); // 未
            public static readonly CueData ZeroFighter_RideSpawn = new CueData(SheetName, "SE_ZeroFighter_RideSpawn"); // 未
            public static readonly CueData Tyranno_Footstep = new CueData(SheetName, "SE_Tyranno_Footstep");
            public static readonly CueData Tyranno_Bite = new CueData(SheetName, "SE_Tyranno_Bite");
            public static readonly CueData Tyranno_Interact = new CueData(SheetName, "SE_Tyranno_Interact");
            public static readonly CueData Hulk_Land = new CueData(SheetName, "SE_Hulk_Land");
            public static readonly CueData Painting_Warp_In = new CueData(SheetName, "SE_Painting_Warp_In");
            public static readonly CueData Painting_Warp_Out = new CueData(SheetName, "SE_Painting_Warp_Out");
            public static readonly CueData Player_Hit_Generic = new CueData(SheetName, "SE_Player_Hit_Generic");
            public static readonly CueData UI_CountDown_Count = new CueData(SheetName, "SE_UI_CountDown_Count");
            public static readonly CueData UI_GameFinish = new CueData(SheetName, "SE_UI_GameFinish");
            public static readonly CueData BokeBoke_Interact = new CueData(SheetName, "SE_BokeBoke_Interact");
            public static readonly CueData UI_CountDown_End = new CueData(SheetName, "SE_UI_CountDown_End");
            public static readonly CueData Car_Interact = new CueData(SheetName, "SE_Car_Interact");
            public static readonly CueData Hatano_Aim = new CueData(SheetName, "SE_Hatano_Aim");
            public static readonly CueData Hatano_Shoot = new CueData(SheetName, "SE_Hatano_Shoot");
            public static readonly CueData Hatano_Ult_1 = new CueData(SheetName, "SE_Hatano_Ult_1");
            public static readonly CueData Hatano_Ult_2 = new CueData(SheetName, "SE_Hatano_Ult_2");
            public static readonly CueData Candle_Interact = new CueData(SheetName, "SE_Candle_Interact");
            public static readonly CueData Candle_Burning = new CueData(SheetName, "SE_Candle_Burning");
            public static readonly CueData Kraken_Interact = new CueData(SheetName, "SE_Kraken_Interact");
            public static readonly CueData Kraken_Attack = new CueData(SheetName, "SE_Kraken_Attack");
            public static readonly CueData Excalibur_Interact = new CueData(SheetName, "SE_Excalibur_Interact");
            public static readonly CueData Excalibur_Attack = new CueData(SheetName, "SE_Excalibur_Attack");
            public static readonly CueData Excalibur_Hit = new CueData(SheetName, "SE_Excalibur_Hit");
            public static readonly CueData Excalibur_Impact = new CueData(SheetName, "SE_Excalibur_Impact");
            public static readonly CueData TreasureChest_Interact = new CueData(SheetName, "SE_TreasureChest_Interact");
            public static readonly CueData NauticalChart_Interact = new CueData(SheetName, "SE_NauticalChart_Interact");
            public static readonly CueData NauticalChart = new CueData(SheetName, "SE_NauticalChart");
            public static readonly CueData Glider_Moving = new CueData(SheetName, "SE_Glider_Moving");
            public static readonly CueData Shark_Attack = new CueData(SheetName, "SE_Shark_Attack");
            public static readonly CueData Shark_Moving = new CueData(SheetName, "SE_Shark_Moving");
            public static readonly CueData Ballista_Hit = new CueData(SheetName, "SE_Ballista_Hit");
            public static readonly CueData Ballista_Attack = new CueData(SheetName, "SE_Ballista_Attack");
            public static readonly CueData Cannon_Attack = new CueData(SheetName, "SE_Cannon_Attack");
            public static readonly CueData Cannon_Hit = new CueData(SheetName, "SE_Cannon_Hit");
            public static readonly CueData Ballista_Interact = new CueData(SheetName, "SE_Ballista_Interact");
            public static readonly CueData Cannon_Interact = new CueData(SheetName, "SE_Cannon_Interact");
            public static readonly CueData Kraken_Impact = new CueData(SheetName, "SE_Kraken_Impact");
            public static readonly CueData Shark_Interact = new CueData(SheetName, "SE_Shark_Interact");
            public static readonly CueData ZipLine_Interact = new CueData(SheetName, "SE_ZipLine_Interact");
            public static readonly CueData Hulk_Ult = new CueData(SheetName, "SE_Hulk_Ult");
            public static readonly CueData Jewel_PickUp = new CueData(SheetName, "SE_Jewel_PickUp");
            public static readonly CueData OKB_Ult = new CueData(SheetName, "SE_OKB_Ult");
            public static readonly CueData Sarutobi_Ult_1 = new CueData(SheetName, "SE_Sarutobi_Ult_1");
            public static readonly CueData Sarutobi_Ult_2 = new CueData(SheetName, "SE_Sarutobi_Ult_2");
            public static readonly CueData SpotLight_Long = new CueData(SheetName, "SE_SpotLight_Long");
            public static readonly CueData SpotLight_Short = new CueData(SheetName, "SE_SpotLight_Short");
            public static readonly CueData Tanihira_Ult_1 = new CueData(SheetName, "SE_Tanihira_Ult_1");
            public static readonly CueData Tanihira_Ult_2 = new CueData(SheetName, "SE_Tanihira_Ult_2");
            public static readonly CueData UI_Cancel = new CueData(SheetName, "SE_UI_Cancel");
            public static readonly CueData UI_Cursor = new CueData(SheetName, "SE_UI_Cursor");
            public static readonly CueData UI_Submit = new CueData(SheetName, "SE_UI_Submit");
            public static readonly CueData Ult_Charge = new CueData(SheetName, "SE_Ult_Charge");
            public static readonly CueData Takamura_Skill = new CueData(SheetName, "SE_Takamura_Skill");
            public static readonly CueData Takamura_Ult = new CueData(SheetName, "SE_Takamura_Ult");
            public static readonly CueData Okubo_Fire = new CueData(SheetName, "SE_Okubo_Fire");
            public static readonly CueData Okubo_Pull = new CueData(SheetName, "SE_Okubo_Pull");
            public static readonly CueData Okubo_Ult_1 = new CueData(SheetName, "SE_Okubo_Ult1");
            public static readonly CueData Okubo_Ult_2 = new CueData(SheetName, "SE_Okubo_Ult2");
            public static readonly CueData Okubo_Aim = new CueData(SheetName, "SE_Okubo_Aim");
            public static readonly CueData Evasion = new CueData(SheetName, "SE_Evasion");
            public static readonly CueData Jewelry_Spawn = new CueData(SheetName, "SE_Jewelry_Spawn");
        }

        public static class VOICE
        {
            public static readonly CueData OKB_CharacterSelect = new CueData(SheetName, "VO_OKB_CharacterSelect");
            public static readonly CueData OKB_GameStart = new CueData(SheetName, "VO_OKB_GameStart");
            public static readonly CueData OKB_Attack_01 = new CueData(SheetName, "VO_OKB_Attack_01");
            public static readonly CueData OKB_Damage_01 = new CueData(SheetName, "VO_OKB_Damage_01");
            public static readonly CueData OKB_Interact_01 = new CueData(SheetName, "VO_OKB_Interact_01");
            public static readonly CueData OKB_UniqueInteract = new CueData(SheetName, "VO_OKB_UniqueInteract"); // 未
            public static readonly CueData OKB_Win = new CueData(SheetName, "VO_OKB_Win");
            public static readonly CueData Haru_CharacterSelect = new CueData(SheetName, "VO_Haru_CharacterSelect");
            public static readonly CueData Haru_GameStart = new CueData(SheetName, "VO_Haru_GameStart");
            public static readonly CueData Haru_Attack_01 = new CueData(SheetName, "VO_Haru_Attack_01");
            public static readonly CueData Haru_Damage_01 = new CueData(SheetName, "VO_Haru_Damage_01");
            public static readonly CueData Haru_Interact_01 = new CueData(SheetName, "VO_Haru_Interact_01");
            public static readonly CueData Haru_Win = new CueData(SheetName, "VO_Haru_Win");
            public static readonly CueData Koinuma_CharacterSelect = new CueData(SheetName, "VO_Koinuma_CharacterSelect");
            public static readonly CueData Koinuma_GameStart = new CueData(SheetName, "VO_Koinuma_GameStart");
            public static readonly CueData Koinuma_Win = new CueData(SheetName, "Koinuma_Win");
            public static readonly CueData Tanihira_CharacterSelect = new CueData(SheetName, "VO_Tanihira_CahracterSelect");
            public static readonly CueData Tanihira_GameStart = new CueData(SheetName, "VO_Tanihira_GameStart");
            public static readonly CueData Tanihira_Attack_01 = new CueData(SheetName, "VO_Tanihira_Attack_01");
            public static readonly CueData Tanihira_Damage_01 = new CueData(SheetName, "VO_Tanihira_Damage_01");
            public static readonly CueData Tanihira_Interact_01 = new CueData(SheetName, "VO_Tanihira_Interact_01");
            public static readonly CueData Tanihira_Win = new CueData(SheetName, "VO_Tanihira_Win");
            public static readonly CueData Hatano_CharacterSelect = new CueData(SheetName, "VO_Hatano_CharacterSelect");
            public static readonly CueData Hatano_InGameStart = new CueData(SheetName, "VO_Hatano_InGameStart");
            public static readonly CueData Hatano_Attack = new CueData(SheetName, "VO_Hatano_Attack");
            public static readonly CueData Hatano_Damage = new CueData(SheetName, "VO_Hatano_Damage");
            public static readonly CueData Hatano_Interact = new CueData(SheetName, "VO_Hatano_Interact");
            public static readonly CueData Hatano_Result = new CueData(SheetName, "VO_Hatano_Result");
            public static readonly CueData Okubo_CharacterSelect = new CueData(SheetName, "VO_Ookubo_CharacterSelect");
            public static readonly CueData Okubo_InGameStart = new CueData(SheetName, "VO_Ookubo_InGameStart");
            public static readonly CueData Okubo_Attack = new CueData(SheetName, "VO_Ookubo_Attack");
            public static readonly CueData Okubo_Damage = new CueData(SheetName, "VO_Ookubo_Damage");
            public static readonly CueData Okubo_Interact = new CueData(SheetName, "VO_Ookubo_Interact");
            public static readonly CueData Okubo_Result = new CueData(SheetName, "VO_Ookubo_Result");
            public static readonly CueData Takamura_CharacterSelect = new CueData(SheetName, "VO_Takamura_CharacterSelect");
            public static readonly CueData Takamura_InGameStart = new CueData(SheetName, "VO_Takamura_InGameStart");
            public static readonly CueData Takamura_Attack = new CueData(SheetName, "VO_Takamura_Attack");
            public static readonly CueData Takamura_Damage = new CueData(SheetName, "VO_Takamura_Damage");
            public static readonly CueData Takamura_Interact = new CueData(SheetName, "VO_Takamura_Interact");
            public static readonly CueData Takamura_Result = new CueData(SheetName, "VO_Takamura_Result");
        }
    }
}

