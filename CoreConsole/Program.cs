using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CoreConsole.APIs;
using PKHeX.Core;

namespace CoreConsole
{
    class ConsoleIndex
    {
        public static PKM pk;
        static void Main(string[] args)
        {
            string appPath = Environment.CurrentDirectory;

         
           if (args.Contains("-server"))
            {
                var server1 = new Server();
                server1.Server_start(7272, "legality_check", false);
                var server2 = new Server();
                server2.Server_start(7273, "info_get", true);
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

    public void Handle_connection(IAsyncResult result)  //the parameter is a delegate, used to communicate between threads
    {
        Accept_connection();  //once again, checking for any other incoming connections
        TcpClient client = server.EndAcceptTcpClient(result);  //creates the TcpClient

        NetworkStream ns = client.GetStream();

        while (client.Connected)  //while the client is connected, we look for incoming messages
        {
            byte[] size = new byte[8];
            ns.Read(size, 0, size.Length);
            string dataSizeStr = Encoding.UTF8.GetString(size, 0, size.Length);

            int.TryParse(dataSizeStr, out int dataSize);

            byte[] msg = new byte[dataSize];     //the messages arrive as byte array
            try
            {
                ns.Read(msg, 0, msg.Length);   //the same networkstream reads the message sent by the client
                var pk = PKMConverter.GetPKMfromBytes(msg);
                if(type == "legality_check")
                {
                    var lc = new LegalityCheck(pk);
                    byte[] report = Encoding.Default.GetBytes(lc.Report);
                    ns.Write(report, 0, report.Length);
                    ns.Flush();
                } else if (type == "info_get")
                {
                    string data = "";
                    data += pk.Nickname.ToString() + "," + pk.OT_Name.ToString() + "," + pk.CurrentLevel.ToString() + "," + pk.Species.ToString() + ",";
                    foreach (int move in pk.GetMoveSet())
                    {
                        data += move.ToString() + ",";
                    }
                    data += pk.Nature.ToString() + "," + pk.IV_HP.ToString() + "," + pk.IV_ATK.ToString() + "," + pk.IV_DEF.ToString() + "," + pk.IV_SPD.ToString() + "," + pk.IV_SPE + "," + pk.IV_SPA
                    + "," + pk.Gender.ToString() + "," + pk.IsShiny.ToString() + "," + pk.Ability.ToString() + "," + pk.HeldItem.ToString() + "," + pk.TID.ToString() + "," + pk.Ball.ToString()
                    + "," + pk.PKRS_Infected.ToString();
                    data += "," + pk.SIZE_STORED.ToString();
                    if(pk.GetType() == typeof(PK4))
                    {
                        data += ",4";
                    } else if (pk.GetType() == typeof(PK5))
                    {
                        data += ",5";

                    } else if(pk.GetType() == typeof(PK6))
                    {
                        data += ",6";
                    } else if(pk.GetType() == typeof(PK7))
                    {
                        data += ",7";
                    } else if (pk.GetType() == typeof(PB7))
                    {
                        data += ",LGPE";
                    }
                    byte[] info = Encoding.UTF8.GetBytes(data);
                    ns.Write(info, 0, info.Length);
                    ns.Flush();
                }
                client.Close();
            } catch
            {
                byte[] err = Encoding.Default.GetBytes("Not a Pokemon!");
                ns.Write(err, 0, err.Length);
                ns.Flush();
                client.Close();
            }
        }
    }
}
