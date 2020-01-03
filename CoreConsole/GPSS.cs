using PKHeX.Core;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace CoreConsole
{
    public class GPSSSummary : PKMSummary
    {
        public GPSSSummary(PKM pkm, GameStrings strings) : base(pkm, strings)
        {
        }
        
        public Pokemon CreatePKMN()
        {
            try
            {

                var country = "N/A";
                var region = "N/A";
                var dsregion = "N/A";
                if (CountryID != "N/A" && CountryID != "0")
                {
                    System.Tuple<string, string> cr = GeoLocation.GetCountryRegionText(int.Parse(CountryID), int.Parse(RegionID), "en");
                    country = cr.Item1;
                    region = cr.Item2;
                }
                switch (DSRegionID)
                {
                    case "N/A":
                        dsregion = "None";
                        break;
                    case "0":
                        dsregion = "Japan";
                        break;
                    case "1":
                        dsregion = "North America";
                        break;
                    case "2":
                        dsregion = "Europe";
                        break;
                    case "3":
                        dsregion = "China";
                        break;
                    case "4":
                        dsregion = "Korea";
                        break;
                    case "5":
                        dsregion = "Taiwan";
                        break;
                }
                var lc = new LegalityAnalysis(pkm);
                var pkmn = new Pokemon
                {
                    ATK = ATK,
                    ATK_EV = ATK_EV,
                    ATK_IV = ATK_IV,
                    Ability = Ability,
                    AbilityNum = AbilityNum,
                    AltForms = AltForms,
                    Ball = Ball,
                    Beauty = Beauty,
                    Cool = Cool,
                    Country = country,
                    CountryID = CountryID,
                    Cute = Cute,
                    DEF = DEF,
                    DEF_EV = DEF_EV,
                    DEF_IV = DEF_IV,
                    DSRegion = dsregion,
                    DSRegionID = DSRegionID,
                    EC = EC,
                    ESV = ESV,
                    EXP = EXP,
                    EggLoc = EggLoc,
                    Egg_Day = Egg_Day,
                    Egg_Month = Egg_Month,
                    Egg_Year = Egg_Year,
                    Encounter = Encounter,
                    FatefulFlag = FatefulFlag,
                    Friendship = Friendship,
                    Gender = Gender,
                    GenderFlag = GenderFlag,
                    Size = pkm.SIZE_STORED,
                    HP = HP,
                    HP_EV = HP_EV,
                    HP_IV = HP_IV,
                    HP_Type = HP_Type,
                    HT = pkm.HT_Name,
                    HeldItem = HeldItem,
                    IsEgg = IsEgg,
                    IsNicknamed = IsNicknamed,
                    IsShiny = IsShiny,
                    Legal = Legal,
                    Level = Level,
                    Markings = Markings,
                    MetLevel = MetLevel,
                    MetLoc = MetLoc,
                    Met_Day = Met_Day,
                    Met_Month = Met_Month,
                    Met_Year = Met_Year,
                    Move1 = Move1,
                    Move1_PP = Move1_PP,
                    Move1_PPUp = Move1_PPUp,
                    Move2 = Move2,
                    Move2_PP = Move2_PP,
                    Move2_PPUp = Move2_PPUp,
                    Move3 = Move3,
                    Move3_PP = Move3_PP,
                    Move3_PPUp = Move3_PPUp,
                    Move4 = Move4,
                    Move4_PP = Move4_PP,
                    Move4_PPUp = Move4_PPUp,
                    Nature = Nature,
                    Nickname = Nickname,
                    NotOT = NotOT,
                    OT = OT,
                    OTLang = OTLang,
                    OT_Affection = OT_Affection,
                    OT_Gender = OT_Gender,
                    PID = PID,
                    PKRS_Days = PKRS_Days,
                    PKRS_Strain = PKRS_Strain,
                    Position = Position ?? "",
                    Region = region,
                    RegionID = RegionID,
                    Relearn1 = Relearn1,
                    Relearn2 = Relearn2,
                    Relearn3 = Relearn3,
                    Relearn4 = Relearn4,
                    SID = SID,
                    SPA = SPA,
                    SPA_EV = SPA_EV,
                    SPA_IV = SPA_IV,
                    SPD = SPD,
                    SPD_EV = SPD_EV,
                    SPD_IV = SPD_IV,
                    SPE = SPE,
                    SPE_EV = SPE_EV,
                    SPE_IV = SPE_IV,
                    Sheen = Sheen,
                    Smart = Smart,
                    Species = Species,
                    SpecForm = pkm.SpecForm,
                    TID = TID,
                    TSV = TSV,
                    Tough = Tough,
                    Version = Version,
                    IllegalReasons = lc.Report(),
                    Checksum = Checksum,
                    ItemNum = pkm.HeldItem
                };
                if (pkm.HT_Name == "")
                {
                    pkmn.HT = OT;
                } 
                var ds = FormConverter.GetFormList(pkm.Species, GameInfo.Strings.types, GameInfo.Strings.forms, GameInfo.GenderSymbolUnicode, pkm.Format);
                if (ds.Count() > 1)
                {
                    pkmn.Form = ds[pkmn.AltForms];
                }
                else
                {
                    pkmn.Form = ds[0];
                }
                pkmn.HeldItemSpriteURL = "";
                pkmn.SpeciesSpriteURL = "";
                pkmn.Move1_Type = ConsoleIndex.mt[pkm.Move1].Type;
                pkmn.Move2_Type = ConsoleIndex.mt[pkm.Move2].Type;
                pkmn.Move3_Type = ConsoleIndex.mt[pkm.Move3].Type;
                pkmn.Move4_Type = ConsoleIndex.mt[pkm.Move4].Type;
                if (pkm.GetType() == typeof(PK4))
                {
                    pkmn.Generation = "4";
                }
                else if (pkm.GetType() == typeof(PK5))
                {
                    pkmn.Generation = "5";

                }
                else if (pkm.GetType() == typeof(PK6))
                {
                    pkmn.Generation = "6";
                }
                else if (pkm.GetType() == typeof(PK7))
                {
                    pkmn.Generation = "7";
                }
                else if (pkm.GetType() == typeof(PB7))
                {
                    pkmn.Generation = "LGPE";
                }
                else if (pkm.GetType() == typeof(PK8))
                {
                    pkmn.Generation = "8";
                }
                else if (pkm.GetType() == typeof(PK3))
                {
                    pkmn.Generation = "3";
                }
                else if (pkm.GetType() == typeof(PK2))
                {
                    pkmn.Generation = "2";
                }
                else if (pkm.GetType() == typeof(PK1))
                {
                    pkmn.Generation = "1";
                }
                return pkmn;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
    }
    [DataContract]
    public class Pokemon 
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
        [DataMember] internal string HT;
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
