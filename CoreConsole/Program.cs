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
using QRCoder;

namespace CoreConsole
{
    //"Gen8": [
    class Encounters
    {
        public List<GenLoc> Gen8;
        public List<GenLoc> Gen7;
        public List<GenLoc> Gen6;
        public List<GenLoc> Gen5;
        public List<GenLoc> Gen4;
        public List<GenLoc> Gen3;
        public List<GenLoc> Gen2;
        public List<GenLoc> Gen1;

    }

    class GenLoc
    {
        public string EncounterType;
        public List<Locs> Locations;
    }
    class Locs
    {
        public string Location;
        public List<string> Games;
    }

    class LearnableMove
    {
        public string MoveName;
        public bool Learnable;
    }

    class LegalityReturn
    {
        public string pokemon;
        public string qr;
        public string species;
        public bool ran;
        public bool success;
        public string[] report;
    }
    class ConsoleIndex
    {
        public static PKM pk;
        public static List<MoveType> mt;
        public static string[] moveNames;
        static void Main(string[] args)
        {
            string appPath = Environment.CurrentDirectory;
            mt = File.ReadAllLines("./coreconsole/Moves.csv").Skip(1).Select(m => MoveType.ReadCsv(m)).ToList();
         
           if (args.Contains("-server"))
            {
                // Init the database
                EncounterEvent.RefreshMGDB(string.Empty);
                RibbonStrings.ResetDictionary(GameInfo.Strings.ribbons);
                LegalityAnalysis.MoveStrings = GameInfo.Strings.movelist;
                LegalityAnalysis.SpeciesStrings = GameInfo.Strings.specieslist;
                moveNames = Util.GetMovesList(GameLanguage.DefaultLanguage);
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

    public string GetQueryString(NetworkStream ns)
    {
        byte[] query_size = Read_NS_Data(ns, 8);
        string query_sizeStr = Encoding.UTF8.GetString(query_size, 0, query_size.Length);
        int.TryParse(query_sizeStr, out int qSize);
        byte[] query = Read_NS_Data(ns, qSize);
        string queryStr = Encoding.UTF8.GetString(query, 0, query.Length);
        queryStr = queryStr.Replace("| ", "|").Replace(" | ", "|").Replace(" |", "|");
        return queryStr;
    }

    public PKM GetPokemon(NetworkStream ns)
    {
        try
        {
            byte[] size = Read_NS_Data(ns, 8);
            string dataSizeStr = Encoding.UTF8.GetString(size, 0, size.Length);
            int.TryParse(dataSizeStr, out int dataSize);
            byte[] msg = Read_NS_Data(ns, dataSize);
            var pk = PKMConverter.GetPKMfromBytes(msg);
            return pk;
        } catch (Exception e)
        {
            Console.WriteLine("fuck!");
            Console.WriteLine(e.ToString());
            return null;
        }

    }
    public GameVersion GetPlaceholderVersion(PKM pkm)
    {
        GameVersion version = 0;
        if (pkm.GetType() == typeof(PK4))
        {
            version = GameVersion.HG;
        }
        else if (pkm.GetType() == typeof(PK5))
        {
            version = GameVersion.B2;

        }
        else if (pkm.GetType() == typeof(PK6))
        {
            version = GameVersion.OR;
        }
        else if (pkm.GetType() == typeof(PK7))
        {
            version = GameVersion.UM;
        }
        else if (pkm.GetType() == typeof(PB7))
        {
            version = GameVersion.GE;
        }
        else if (pkm.GetType() == typeof(PK8))
        {
            version = GameVersion.SW;
        }
        else if (pkm.GetType() == typeof(PK3))
        {
            version = GameVersion.E;
        }
        else if (pkm.GetType() == typeof(PK2))
        {
            version = GameVersion.C;
        }
        else if (pkm.GetType() == typeof(PK1))
        {
            version = GameVersion.YW;
        }
        return version;
    }
    public byte[] GenQR(PKM pokemon)
    {
        try
        {
            string data = QRMessageUtil.GetMessage(pokemon);
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.L);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(3);
            qrCode.Dispose();
            qrGenerator.Dispose();
            return qrCodeAsPngByteArr;
        } catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return null;
        }

    }
    public void Handle_connection(IAsyncResult result)  //the parameter is a delegate, used to communicate between threads
    {
        Accept_connection();  //once again, checking for any other incoming connections
        TcpClient client = server.EndAcceptTcpClient(result);  //creates the TcpClient

        NetworkStream ns = client.GetStream();

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
                    var pk = GetPokemon(ns);
                    if (pk == null)
                    {
                        throw new System.ArgumentException("There was an issue reading the Pokemon data, is it not a Pokemon? (or maybe timeout?)");
                    }
                    if (type == "legality_check")
                    {
                        var lc = new LegalityAnalysis(pk);

                        byte[] report = Encoding.Default.GetBytes(lc.Report());
                        ns.Write(report, 0, report.Length);
                    }
                    else if (type == "info_get")
                    {

                        var summary = new GPSSSummary(pk, GameInfo.Strings);
                        var pkmn = summary.CreatePKMN();
                        if (pkmn == null)
                        {
                            throw new System.ArgumentException("There was an issue reading the Pokemon data, is it not a Pokemon? (or maybe timeout?)");
                        }
                        var ser = new DataContractJsonSerializer(typeof(Pokemon));
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
                    switch (request_typeStr) {
                        case "enc_base64_get":
                            {
                                var qr = GenQR(GetPokemon(ns));
                                if (qr == null || qr.Length == 0) { 
                                    ns.Write(Encoding.UTF8.GetBytes("."), 0, Encoding.UTF8.GetBytes(".").Length);
                                    //throw new System.ArgumentException("Tried to upload something that wasn't a pokemon or something else went wrong during qr generation!");
                                } else
                                {
                                    ns.Write(qr, 0, qr.Length);
                                }
                                break;
                            }
                        case "encounter":
                            {
                                var queryStr = GetQueryString(ns);
                                
                                var queries = queryStr.Split('|');

                                if(!Enum.GetNames(typeof(Species)).Any(s => s.ToLower() == queries[0]))
                                {
                                    throw new System.ArgumentException("Invalid pokemon name provided!");
                                }
                                var data = EncounterLearn.GetLearnSummary(queries[0], queries.Skip(1));
                                var e = new Encounters
                                {
                                    Gen1 = new List<GenLoc>(),
                                    Gen2 = new List<GenLoc>(),
                                    Gen3 = new List<GenLoc>(),
                                    Gen4 = new List<GenLoc>(),
                                    Gen5 = new List<GenLoc>(),
                                    Gen6 = new List<GenLoc>(),
                                    Gen7 = new List<GenLoc>(),
                                    Gen8 = new List<GenLoc>(),
                                };
                                bool first = true;
                                var enc = "";
                                foreach (var line in data)
                                {
                                    if (line.StartsWith("="))
                                    {
                                        if (!first)
                                        {
                                        }
                                        enc = line.Replace("=", "");
                                        first = false;
                                        continue;
                                    }
                                    var gen = GetStringFromRegex(@"Gen[0-9]", line);
                                    var loc = GetStringFromRegex(@"(?<=.{8}).+?(?=:)", line);
                                    var games = GetStringFromRegex(@"([\t ][A-Z |,]{1,100}$|Any)", line);
                                    games = games.Replace(" ", "");
                                    games = games.Trim(':');
                                    games = games.Trim('\t');
                                    string[] gamesArray = games.Split(',');
                                    GenLoc entry = new GenLoc();
                                    switch (gen)
                                    {
                                        case "Gen1":
                                            {
                                                entry = e.Gen1.FirstOrDefault(l => l.EncounterType == enc);
                                                break;
                                            }
                                        case "Gen2":
                                            {
                                                entry = e.Gen2.FirstOrDefault(l => l.EncounterType == enc);
                                                break;
                                            }

                                        case "Gen3":
                                            {
                                                entry = e.Gen3.FirstOrDefault(l => l.EncounterType == enc);
                                                break;
                                            }
                                        
                                        case "Gen4":
                                            {
                                                entry = e.Gen4.FirstOrDefault(l => l.EncounterType == enc);
                                                break;
                                            }
                                        case "Gen5":
                                            {
                                                entry = e.Gen5.FirstOrDefault(l => l.EncounterType == enc);
                                                break;
                                            }
                                        case "Gen6":
                                            {
                                                entry = e.Gen6.FirstOrDefault(l => l.EncounterType == enc);
                                                break;
                                            }
                                        case "Gen7":
                                            {
                                                entry = e.Gen7.FirstOrDefault(l => l.EncounterType == enc);
                                                break;
                                            }
                                        case "Gen8":
                                            {
                                                entry = e.Gen8.FirstOrDefault(l => l.EncounterType == enc);
                                                break;
                                            }

                                    }
                                    if (entry != null)
                                    {
                                        if (entry.Locations == null)
                                        {
                                            entry.Locations = new List<Locs>();
                                        }
                                        var tmpGamesList = new List<string>();
                                        foreach (var game in gamesArray)
                                        {
                                            tmpGamesList.Add(game);
                                        }
                                        entry.Locations.Add(new Locs
                                        {

                                            Location = loc,
                                            Games = tmpGamesList,
                                        });
                                    }
                                    else
                                    {
                                        var tmpGamesList = new List<string>();
                                        foreach (var game in gamesArray)
                                        {
                                            tmpGamesList.Add(game);
                                        }
                                        var tmpLocations = new List<Locs>
                                        {
                                            new Locs
                                            {
                                                Location = loc,
                                                Games = tmpGamesList,
                                            }
                                        };
                                        var tmpGenLoc = new GenLoc
                                        {
                                            EncounterType = enc,
                                            Locations = tmpLocations,
                                        };
                                        switch (gen)
                                        {
                                            case "Gen1":
                                                {
                                                    e.Gen1.Add(tmpGenLoc);
                                                    break;
                                                }
                                            case "Gen2":
                                                {
                                                    e.Gen2.Add(tmpGenLoc);
                                                    break;
                                                }

                                            case "Gen3":
                                                {
                                                    e.Gen3.Add(tmpGenLoc);
                                                    break;
                                                }

                                            case "Gen4":
                                                {
                                                    e.Gen4.Add(tmpGenLoc);
                                                    break;
                                                }
                                            case "Gen5":
                                                {
                                                    e.Gen5.Add(tmpGenLoc);
                                                    break;
                                                }
                                            case "Gen6":
                                                {
                                                    e.Gen6.Add(tmpGenLoc);
                                                    break;
                                                }
                                            case "Gen7":
                                                {
                                                    e.Gen7.Add(tmpGenLoc);
                                                    break;
                                                }
                                            case "Gen8":
                                                {
                                                    e.Gen8.Add(tmpGenLoc);
                                                    break;
                                                }

                                        }
                                    }
                                }
                                var json = JsonConvert.SerializeObject(e);
                                ns.Write(Encoding.UTF8.GetBytes(json), 0, Encoding.UTF8.GetBytes(json).Length);
                                break;
                            }
                        case "move_learn":
                            {
                                var queryStr = GetQueryString(ns);
                                var queries = queryStr.Split('|');
                                if (!Enum.GetNames(typeof(Species)).Any(s => s.ToLower() == queries[0]))
                                {
                                    throw new System.ArgumentException("Invalid pokemon name provided!");
                                }
                                var i = 1;
                                var moves = new List<LearnableMove>();
                                foreach (var move in queries.Skip(1))
                                {
                                    if (i > 4)
                                    {
                                        break;
                                    }
                                    var workaround = move.Split(',');
                                    var tmpMove = new LearnableMove
                                    {
                                        MoveName = move,
                                    };
                                    if (!ConsoleIndex.moveNames.Any(m => m.ToLower().Contains(move.ToLower()))){
                                        tmpMove.Learnable = false;
                                    } else
                                    {
                                        tmpMove.Learnable = bool.Parse(EncounterLearn.CanLearn(queries[0], workaround).ToString());
                                    }
                                    moves.Add(tmpMove);
                                    i++;
                                }
                                var json = JsonConvert.SerializeObject(moves);
                                ns.Write(Encoding.UTF8.GetBytes(json), 0, Encoding.UTF8.GetBytes(json).Length);
                                break;
                            }
                        case "poke_info":
                            {
                                var summary = new GPSSSummary(GetPokemon(ns), GameInfo.Strings);
                                var pkmn = summary.CreatePKMN();
                                if (pkmn == null)
                                {
                                    throw new System.ArgumentException("There was an issue reading the Pokemon data, is it not a Pokemon? (or maybe timeout?)");
                                }
                                var ser = new DataContractJsonSerializer(typeof(Pokemon));
                                ser.WriteObject(ns, pkmn);
                                break;
                            }
                        case "auto_legality":
                            {
                                byte[] size = Read_NS_Data(ns, 8);
                                string dataSizeStr = Encoding.UTF8.GetString(size, 0, size.Length);
                                int.TryParse(dataSizeStr, out int dSize);
                                byte[] version = Read_NS_Data(ns, dSize);
                                string versionStr = Encoding.UTF8.GetString(version, 0, version.Length);
                                GameVersion versionInt = 0;

                                var pkmn = GetPokemon(ns);
                                if (pkmn == null)
                                {
                                    throw new System.ArgumentException("There was an issue reading the Pokemon data, is it not a Pokemon? (or maybe timeout?)");
                                }
                                if (versionStr == "?")
                                {
                                    versionInt = GetPlaceholderVersion(pkmn);
                                }
                                else
                                {
                                    int.TryParse(versionStr, out int vInt);
                                    versionInt = (GameVersion)vInt;
                                }

                                var lc = new LegalityAnalysis(pkmn);
                                var report = lc.Report().Split('\n');
                                var data = new LegalityReturn
                                {
                                    ran = false,
                                    success = true
                                };
                                if (!lc.Valid)
                                {
                                    var alm = new AutoLegality(pkmn, versionInt.ToString());
                                    Console.WriteLine(pkmn.Version);
                                    var legalPK = alm.Legalize(pkmn, versionInt);
                                    var qr = System.Convert.ToBase64String(GenQR(legalPK)); 
                                    if(qr == null || qr.Length == 0)
                                    {
                                        throw new ArgumentException("bad pokemon!");
                                    }
                                    data.qr = System.Convert.ToBase64String(GenQR(legalPK));
                                    data.pokemon = System.Convert.ToBase64String(legalPK.DecryptedBoxData);
                                    data.ran = true;
                                    data.species = new GPSSSummary(legalPK, GameInfo.Strings).Species;
                                    data.success = new LegalityAnalysis(legalPK).Valid;
                                }
                                if (data.ran && data.success)
                                {
                                    data.report = report;
                                }
                                var json = JsonConvert.SerializeObject(data);
                                ns.Write(Encoding.UTF8.GetBytes(json), 0, Encoding.UTF8.GetBytes(json).Length);
                                break;
                            }
             /*           case "index_lookup":
                            {
                                var queryStr = GetQueryString(ns);
                                break;
                            }*/
                         default: {
                                    ns.Write(Encoding.UTF8.GetBytes("I don't know how to handle this query type yet!"), 0, Encoding.UTF8.GetBytes("I don't know how to handle this query type yet!").Length);
                                    break;
                                }

                    }
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                byte[] err = Encoding.Default.GetBytes("Not a Pokemon!");
                ns.Write(err, 0, err.Length);
            } finally
            {
                ns.Flush();
                ns.Dispose();
                client.Dispose();
            }
        }
    }
}
