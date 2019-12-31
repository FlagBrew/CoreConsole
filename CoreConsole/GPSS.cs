using PKHeX.Core;
using System;
using System.Runtime.Serialization;

namespace CoreConsole
{
    public class GPSSSummary : PKMSummary
    {
        public GPSSSummary(PKM pkm, GameStrings strings) : base(pkm, strings)
        {
           
        }
    }
    [DataContract]
    internal class Pokemon 
    {
        [DataMember] internal string Generation;
        [DataMember] internal int Size;
        [DataMember] internal bool IsShiny;
        [DataMember] internal bool IsNicknamed;
        [DataMember] internal bool IsEgg;
        [DataMember] internal bool FatefulFlag;
        [DataMember] internal int OT_Gender;
        [DataMember] internal int MetLevel;
        [DataMember] internal int PKRS_Days;
        [DataMember] internal int PKRS_Strain;
        [DataMember] internal int AltForms;
        [DataMember] internal int GenderFlag;
        [DataMember] internal int AbilityNum;
        [DataMember] internal string NotOT;
        [DataMember] internal int Markings;
        [DataMember] internal int Sheen;
        [DataMember] internal int Tough;
        [DataMember] internal int Smart;
        [DataMember] internal int Cute;
        [DataMember] internal int Beauty;
        [DataMember] internal int TID;
        [DataMember] internal int SID;
        [DataMember] internal int TSV;
        [DataMember] internal int Move1_PP;
        [DataMember] internal int Met_Month;
        [DataMember] internal int Met_Year;
        [DataMember] internal int Egg_Day;
        [DataMember] internal int Egg_Month;
        [DataMember] internal int Egg_Year;
        [DataMember] internal int OT_Affection;
        [DataMember] internal int Friendship;
        [DataMember] internal int SpecForm;
        [DataMember] internal ushort Checksum;
        [DataMember] internal string Relearn4;
        [DataMember] internal string Relearn3;
        [DataMember] internal string Relearn2;
        [DataMember] internal string Relearn1;
        [DataMember] internal int Move4_PPUp;
        [DataMember] internal int Move3_PPUp;
        [DataMember] internal int Move2_PPUp;
        [DataMember] internal int Move1_PPUp;
        [DataMember] internal int Move4_PP;
        [DataMember] internal int Move3_PP;
        [DataMember] internal int Move2_PP;
        [DataMember] internal int Cool;
        [DataMember] internal int SPE_EV;
        [DataMember] internal int SPD_EV;
        [DataMember] internal int SPA_EV;
        [DataMember] internal string SPE;
        [DataMember] internal string SPD;
        [DataMember] internal string SPA;
        [DataMember] internal string DEF;
        [DataMember] internal string ATK;
        [DataMember] internal string HP;
        [DataMember] internal string HeldItem;
        [DataMember] internal string Move4;
        [DataMember] internal string Move3;
        [DataMember] internal string Move2;
        [DataMember] internal string Move1;
        [DataMember] internal string Move1_Type;
        [DataMember] internal string Move2_Type;
        [DataMember] internal string Move3_Type;
        [DataMember] internal string Move4_Type;
        [DataMember] internal string Ability;
        [DataMember] internal string HP_Type;
        [DataMember] internal string ESV;
        [DataMember] internal string Gender;
        [DataMember] internal string Nature;
        [DataMember] internal string Species;
        [DataMember] internal string Nickname;
        [DataMember] internal string Position;
        [DataMember] internal string MetLoc;
        [DataMember] internal int Met_Day;
        [DataMember] internal string EggLoc;
        [DataMember] internal string OT;
        [DataMember] internal int DEF_EV;
        [DataMember] internal int ATK_EV;
        [DataMember] internal int HP_EV;
        [DataMember] internal int Level;
        [DataMember] internal uint EXP;
        [DataMember] internal int SPE_IV;
        [DataMember] internal int SPD_IV;
        [DataMember] internal int SPA_IV;
        [DataMember] internal int DEF_IV;
        [DataMember] internal int ATK_IV;
        [DataMember] internal int HP_IV;
        [DataMember] internal string PID;
        [DataMember] internal string EC;
        [DataMember] internal string DSRegionID;
        [DataMember] internal string DSRegion;
        [DataMember] internal string RegionID;
        [DataMember] internal string Region;
        [DataMember] internal string CountryID;
        [DataMember] internal string Country;
        [DataMember] internal string Legal;
        [DataMember] internal string OTLang;
        [DataMember] internal string Version;
        [DataMember] internal string Ball;
        [DataMember] internal int Encounter;
        [DataMember] internal string Form;
        [DataMember] internal string IllegalReasons;
        [DataMember] internal int ItemNum;
        [DataMember] internal string SpeciesSpriteURL;
        [DataMember] internal string HeldItemSpriteURL;
    }




}
