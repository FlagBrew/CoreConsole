using System;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace CoreConsole.APIs
{
    class AutoLegality
    {
        public static LegalityCheck lc;
        public static bool Legal => lc.Legal;
        public static PKM legalpk;
        private readonly bool debug = true;

        public AutoLegality(PKM pk, string ver)
        {
            bool valid = Enum.TryParse<GameVersion>(ver, true, out var game);
            if (valid)
                ProcessALM(pk, game);
            return;
        }

        public void ProcessALM(PKM pkm, GameVersion ver = GameVersion.GP)
        {
            lc = new LegalityCheck(pkm);
            if (Legal)
                legalpk = pkm;
            else
                legalpk = Legalize(pkm, ver);
        }

        public PKM Legalize(PKM pk, GameVersion ver)
        {
            var ot_name = pk.OT_Name;
            var ht_name = pk.HT_Name;
  
            var keep_original_data = true;
            if (debug) Console.WriteLine(lc.Report);
            if (lc.Report.ToLower().Contains("wordfilter") || lc.Report.Contains("SID") || lc.Report.Contains("TID"))
            {
                keep_original_data = false;
                ht_name = "PKHeX";
            }
            var sav = SaveUtil.GetBlankSAV(ver, ht_name);
            var updated = sav.Legalize(pk);
            var new_sid = updated.SID;
            var old_new_name = updated.OT_Name;
            var new_tid = updated.TID;
            if (keep_original_data)
            {
                updated.TID = pk.TID;
                updated.SID = pk.SID;
                updated.OT_Name = ot_name;

            }
            lc = new LegalityCheck(updated);
            if (Legal)
            {
                legalpk = updated;
            } else
            {
                if (keep_original_data)
                {
                    updated.TID = new_tid;
                    updated.OT_Name = old_new_name;
                    updated.SID = new_sid;
                    lc = new LegalityCheck(updated);
                    if (Legal)
                    {
                        legalpk = updated;
                    }
                }
            }
            return updated;
        }

        public PKM GetLegalPKM()
        {
            return legalpk;
        }
    }
}
