using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using CoreConsole;
using CoreConsole.APIs;
using Newtonsoft.Json;
using PKHeX.Core;

namespace CoreConsole
{

    class Encounters
    {
        public string EncounterType;
        public List<EncounterGens> Generations;
    }

    class EncounterGens
    {
        public string Generation;
        public List<GameLoc> Locations;
    }

    class GameLoc
    {
        public string Location;
        public List<string> Games;
    }
    class ConsoleIndex
    {
        public static PKM pk;
        public static List<MoveType> mt;
        static void Main(string[] args)
        {
            string appPath = Environment.CurrentDirectory;
            mt = File.ReadAllLines("./coreconsole/Moves.csv").Skip(1).Select(m => MoveType.ReadCsv(m)).ToList();
         
           if (args.Contains("-server"))
            {
                // Init the database
                Legal.RefreshMGDB(string.Empty);
                RibbonStrings.ResetDictionary(GameInfo.Strings.ribbons);
                LegalityAnalysis.MoveStrings = GameInfo.Strings.movelist;
                LegalityAnalysis.SpeciesStrings = GameInfo.Strings.specieslist;
                Util.GetStringList("countries");
                // Init Move Types DB

                var server1 = new Server();
                server1.Server_start(7272, "legality_check", false);
                var server2 = new Server();
                server2.Server_start(7273, "info_get", false);
                var server3 = new Server();
                server3.Server_start(7274, "legal_fix", false);
                var server4 = new Server();
                server4.Server_start(7275, "bot_info", true);
            }
            else
            {
                Initialize(args);
                if (args.Contains("-l"))
                {
                    // Legality API calls
                    var lc = new LegalityCheck(pk);
                    if (args.Contains("--verbose")) Console.WriteLine(lc.VerboseReport);
                    else Console.WriteLine(lc.Report);
                }
                if (args.Contains("-alm"))
                {
                    if (!args.Contains("--version")) Console.WriteLine("Specify version with the [--version] tag");
                    else
                    {
                        var alm = new AutoLegality(pk, args[Array.IndexOf(args, "--version") + 1]);
                        if (alm != null)
                        {
                            if (!args.Contains("-o"))
                            {
                                string output = Util.CleanFileName(alm.GetLegalPKM().FileName);
                                File.WriteAllBytes(Path.Combine(appPath, "output", output), alm.GetLegalPKM().DecryptedBoxData);
                            }
                            else
                            {
                                string output = GetOutputPath(args);
                                File.WriteAllBytes(output, alm.GetLegalPKM().DecryptedBoxData);
                            }
                        }
                        else Console.WriteLine("Invalid version");
                    }
                }
            }
        }
        
        private static void Initialize(string[] args)
        {
            // check -i for input and get file path in the next arg
            if (args.Contains("-i"))
            {
                string path = GetFilePath(args);
                byte[] data = File.ReadAllBytes(path);
                pk = PKMConverter.GetPKMfromBytes(data);
            }
            else
            {
                if(Array.IndexOf(args, "--set") == -1)
                {
                    Console.WriteLine("Missing arguments!");
                    Environment.Exit(1);
                }
                string set = args[Array.IndexOf(args, "--set") + 1];
                _ = Enum.TryParse<GameVersion>(args[Array.IndexOf(args, "--version") + 1], true, out var game);
                var template = PKMConverter.GetBlank(game.GetGeneration(), game);
                template.ApplySetDetails(new ShowdownSet(set.Split(new string[] { "\\n" }, StringSplitOptions.None)));
                pk = template;
            }
        }

        private static string GetFilePath(string[] args)
        {
            return args[Array.IndexOf(args, "-i") + 1];
        }
        
        private static string GetOutputPath(string[] args)
        {
            return args[Array.IndexOf(args, "-o") + 1];
        }
    }
}

public class Server
{
    TcpListener server;
    String type = "";
    public void Server_start(int port, string server_type, bool wait_loop)
    {
        server = new TcpListener(IPAddress.Any, port);
        type = server_type;
        server.Start();
        Accept_connection();  //accepts incoming connections
        Console.WriteLine("Started " + type + " Server!");
        if (wait_loop)
        {
            while (true)
            {
                Thread.Sleep(5000);
            }
        }
    }


    private void Accept_connection()
    {
        server.BeginAcceptTcpClient(Handle_connection, server);  //this is called asynchronously and will run in a different thread
    }

    public byte[] Read_NS_Data(NetworkStream ns, int size)
    {
        byte[] msg = new byte[size];
        ns.Read(msg, 0, msg.Length);
        return msg;
    }

    public string GetStringFromRegex(string regex_pattern, string source)
    {
        Regex r = new Regex(regex_pattern);
        if (r.Match(source).Success)
        {
            return r.Match(source).Value;
        }
        return "";
    }

    public void Handle_connection(IAsyncResult result)  //the parameter is a delegate, used to communicate between threads
    {
        Accept_connection();  //once again, checking for any other incoming connections
        TcpClient client = server.EndAcceptTcpClient(result);  //creates the TcpClient

        NetworkStream ns = client.GetStream();
        var ser = new DataContractJsonSerializer(typeof(Pokemon));

        while (client.Connected)  //while the client is connected, we look for incoming messages
        {
            try
            {
                if (type != "bot_info")
                {


                    string version = "";
                    if (type == "legal_fix")
                    {
                        byte[] versionBytes = Read_NS_Data(ns, 8);
                        version = Encoding.UTF8.GetString(versionBytes, 0, versionBytes.Length);
                        Console.WriteLine("Version sent over is: " + version);
                    }


                    byte[] size = Read_NS_Data(ns, 8);
                    string dataSizeStr = Encoding.UTF8.GetString(size, 0, size.Length);

                    int.TryParse(dataSizeStr, out int dataSize);
                    byte[] msg = Read_NS_Data(ns, dataSize);
                    var pk = PKMConverter.GetPKMfromBytes(msg);
                    if (type == "legality_check")
                    {
                        var lc = new LegalityAnalysis(pk);

                        byte[] report = Encoding.Default.GetBytes(lc.Report());
                        ns.Write(report, 0, report.Length);
                    }
                    else if (type == "info_get")
                    {

                        var strings = GameInfo.Strings;
                        var summary = new GPSSSummary(pk, strings);
                        var country = "N/A";
                        var region = "N/A";
                        var dsregion = "N/A";
                        if (summary.CountryID != "N/A" && summary.CountryID != "0")
                        {
                            System.Tuple<string, string> cr = GeoLocation.GetCountryRegionText(int.Parse(summary.CountryID), int.Parse(summary.RegionID), "en");
                            country = cr.Item1;
                            region = cr.Item2;
                        }
                        switch (summary.DSRegionID)
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
                        var lc = new LegalityAnalysis(pk);
                        var pkmn = new Pokemon
                        {
                            ATK = summary.ATK,
                            ATK_EV = summary.ATK_EV,
                            ATK_IV = summary.ATK_IV,
                            Ability = summary.Ability,
                            AbilityNum = summary.AbilityNum,
                            AltForms = summary.AltForms,
                            Ball = summary.Ball,
                            Beauty = summary.Beauty,
                            Cool = summary.Cool,
                            Country = country,
                            CountryID = summary.CountryID,
                            Cute = summary.Cute,
                            DEF = summary.DEF,
                            DEF_EV = summary.DEF_EV,
                            DEF_IV = summary.DEF_IV,
                            DSRegion = dsregion,
                            DSRegionID = summary.DSRegionID,
                            EC = summary.EC,
                            ESV = summary.ESV,
                            EXP = summary.EXP,
                            EggLoc = summary.EggLoc,
                            Egg_Day = summary.Egg_Day,
                            Egg_Month = summary.Egg_Month,
                            Egg_Year = summary.Egg_Year,
                            Encounter = summary.Encounter,
                            FatefulFlag = summary.FatefulFlag,
                            Friendship = summary.Friendship,
                            Gender = summary.Gender,
                            GenderFlag = summary.GenderFlag,
                            Size = pk.SIZE_STORED,
                            HP = summary.HP,
                            HP_EV = summary.HP_EV,
                            HP_IV = summary.HP_IV,
                            HP_Type = summary.HP_Type,
                            HeldItem = summary.HeldItem,
                            IsEgg = summary.IsEgg,
                            IsNicknamed = summary.IsNicknamed,
                            IsShiny = summary.IsShiny,
                            Legal = summary.Legal,
                            Level = summary.Level,
                            Markings = summary.Markings,
                            MetLevel = summary.MetLevel,
                            MetLoc = summary.MetLoc,
                            Met_Day = summary.Met_Day,
                            Met_Month = summary.Met_Month,
                            Met_Year = summary.Met_Year,
                            Move1 = summary.Move1,
                            Move1_PP = summary.Move1_PP,
                            Move1_PPUp = summary.Move1_PPUp,
                            Move2 = summary.Move2,
                            Move2_PP = summary.Move2_PP,
                            Move2_PPUp = summary.Move2_PPUp,
                            Move3 = summary.Move3,
                            Move3_PP = summary.Move3_PP,
                            Move3_PPUp = summary.Move3_PPUp,
                            Move4 = summary.Move4,
                            Move4_PP = summary.Move4_PP,
                            Move4_PPUp = summary.Move4_PPUp,
                            Nature = summary.Nature,
                            Nickname = summary.Nickname,
                            NotOT = summary.NotOT,
                            OT = summary.OT,
                            OTLang = summary.OTLang,
                            OT_Affection = summary.OT_Affection,
                            OT_Gender = summary.OT_Gender,
                            PID = summary.PID,
                            PKRS_Days = summary.PKRS_Days,
                            PKRS_Strain = summary.PKRS_Strain,
                            Position = summary.Position ?? "",
                            Region = region,
                            RegionID = summary.RegionID,
                            Relearn1 = summary.Relearn1,
                            Relearn2 = summary.Relearn2,
                            Relearn3 = summary.Relearn3,
                            Relearn4 = summary.Relearn4,
                            SID = summary.SID,
                            SPA = summary.SPA,
                            SPA_EV = summary.SPA_EV,
                            SPA_IV = summary.SPA_IV,
                            SPD = summary.SPD,
                            SPD_EV = summary.SPD_EV,
                            SPD_IV = summary.SPD_IV,
                            SPE = summary.SPE,
                            SPE_EV = summary.SPE_EV,
                            SPE_IV = summary.SPE_IV,
                            Sheen = summary.Sheen,
                            Smart = summary.Smart,
                            Species = summary.Species,
                            SpecForm = pk.SpecForm,
                            TID = summary.TID,
                            TSV = summary.TSV,
                            Tough = summary.Tough,
                            Version = summary.Version,
                            IllegalReasons = lc.Report(),
                            Checksum = summary.Checksum,
                            ItemNum = pk.HeldItem
                        };
                        var ds = FormConverter.GetFormList(pk.Species, GameInfo.Strings.types, GameInfo.Strings.forms, GameInfo.GenderSymbolUnicode, pk.Format);
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
                        pkmn.Move1_Type = ConsoleIndex.mt[pk.Move1].Type;
                        pkmn.Move2_Type = ConsoleIndex.mt[pk.Move2].Type;
                        pkmn.Move3_Type = ConsoleIndex.mt[pk.Move3].Type;
                        pkmn.Move4_Type = ConsoleIndex.mt[pk.Move4].Type;
                        if (pk.GetType() == typeof(PK4))
                        {
                            pkmn.Generation = "4";
                        }
                        else if (pk.GetType() == typeof(PK5))
                        {
                            pkmn.Generation = "5";

                        }
                        else if (pk.GetType() == typeof(PK6))
                        {
                            pkmn.Generation = "6";
                        }
                        else if (pk.GetType() == typeof(PK7))
                        {
                            pkmn.Generation = "7";
                        }
                        else if (pk.GetType() == typeof(PB7))
                        {
                            pkmn.Generation = "LGPE";
                        }
                        else if (pk.GetType() == typeof(PK8))
                        {
                            pkmn.Generation = "8";
                        }
                        else if (pk.GetType() == typeof(PK3))
                        {
                            pkmn.Generation = "3";
                        }
                        else if (pk.GetType() == typeof(PK2))
                        {
                            pkmn.Generation = "2";
                        }
                        else if (pk.GetType() == typeof(PK1))
                        {
                            pkmn.Generation = "1";
                        }
                        ser.WriteObject(ns, pkmn);
                    }
                    else if (type == "legal_fix")
                    {

                        var alm = new AutoLegality(pk, version);
                        ns.Write(alm.GetLegalPKM().DecryptedBoxData, 0, alm.GetLegalPKM().DecryptedBoxData.Length);

                    }
                } else
                {
                    byte[] request_string_size = Read_NS_Data(ns, 10);
                    string request_string_sizeStr = Encoding.UTF8.GetString(request_string_size, 0, request_string_size.Length);

                    int.TryParse(request_string_sizeStr, out int dataSize);
                    byte[] request_type = Read_NS_Data(ns, dataSize);
                    string request_typeStr = Encoding.UTF8.GetString(request_type, 0, request_type.Length);
                    if(request_typeStr == "enc_base64_get")
                    {
                        Console.WriteLine("Bot requested a base64 string of an encrypted pokemon! Let's do it!");


                        byte[] size = Read_NS_Data(ns, 8);
                        string dataSizeStr = Encoding.UTF8.GetString(size, 0, size.Length);
                        int.TryParse(dataSizeStr, out int dSize);
                        byte[] msg = Read_NS_Data(ns, dSize);
                        var pk = PKMConverter.GetPKMfromBytes(msg);
                        var b64 = System.Convert.ToBase64String(pk.EncryptedBoxData);
                        Console.WriteLine(b64);
                        ns.Write(Encoding.ASCII.GetBytes(b64), 0, b64.Length);

                    } else if(request_typeStr == "encounter")
                    {
                        Console.WriteLine("Encounter lookup!");
                        byte[] pkmn_name_size = Read_NS_Data(ns, 8);
                        string pkmn_name_sizeStr = Encoding.UTF8.GetString(pkmn_name_size, 0, pkmn_name_size.Length);
                        int.TryParse(pkmn_name_sizeStr, out int dSize);
                        byte[] query = Read_NS_Data(ns, dSize);
                        string queryStr = Encoding.UTF8.GetString(query, 0, query.Length);

                        queryStr = queryStr.Replace(", ", ",");
                        var queries = queryStr.Split(',');
                        
                        
                        var data = EncounterLearn.GetLearnSummary(queries[0], queries.Skip(1));
                        List<Encounters> e = new List<Encounters>();
                        Encounters tmpE = new Encounters();
                        List<EncounterGens> tmpGL = new List<EncounterGens>();
                        bool first = true;
                        foreach (var line in data)
                        {
                            if (line.StartsWith("="))
                            {
                                if (!first)
                                {
                                    tmpE.Generations = tmpGL;
                                    e.Add(tmpE);
                                    tmpGL = new List<EncounterGens>();
                                }
                                first = false;
                                tmpE = new Encounters
                                {
                                    EncounterType = line.Replace("=", "")
                                };
                                continue;
                            }
                            var gen = GetStringFromRegex(@"Gen[0-9]", line);
                            var loc = GetStringFromRegex(@"(?<=.{8}).+?(?=:)", line);
                            var games = GetStringFromRegex(@"([\t ][A-Z |,]{1,100}$|Any)", line);
                            games = games.Replace(" ", "");
                            games = games.Trim(':');
                            games = games.Trim('\t');
                            string[] gamesArray = games.Split(',');
                            if (!first)
                            {
                                var tmpLoc = new GameLoc
                                {
                                    Location = loc,
                                    Games = new List<string>()
                                };
                                foreach (var game in gamesArray)
                                {
                                    tmpLoc.Games.Add(game);
                                }
                                var tmp = tmpGL.FirstOrDefault(g => g.Generation == gen);
                                if (tmp != null)
                                {
                                    tmp.Locations.Add(tmpLoc);
                                } else
                                {
                                    EncounterGens tmpEG = new EncounterGens
                                    {
                                        Generation = gen,
                                        Locations = new List<GameLoc>
                                    {
                                        tmpLoc
                                    }
                                    };
                                    tmpGL.Add(tmpEG);
                                }
                            }
                            if (line == data.Last())
                            {
                                tmpE.Generations = tmpGL;
                                e.Add(tmpE);
                            }
                        }
                        var json = JsonConvert.SerializeObject(e);
                        var data_string = string.Join("\n", data.ToArray());
                        ns.Write(Encoding.UTF8.GetBytes(json), 0, Encoding.UTF8.GetBytes(json).Length);
                    }
                    else
                    {
                        Console.WriteLine("I don't know how to handle " + request_typeStr + " yet!");
                    }
                }
                ns.Flush();
                client.Close();
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                byte[] err = Encoding.Default.GetBytes("Not a Pokemon!");
                ns.Write(err, 0, err.Length);
                ns.Flush();
                client.Close();
            }
        }
    }
}
