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
                var server = new Server();
                server.Server_start();
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
    readonly TcpListener server = new TcpListener(IPAddress.Loopback, 7272);
    public void Server_start()
    {
        server.Start();
        Accept_connection();
        Console.WriteLine("Started Server!");
        while (true)
        {
            Thread.Sleep(5000);
        }
    }


    private void Accept_connection()
    {
        server.BeginAcceptTcpClient(Handle_connection, server);
    }

    public void Handle_connection(IAsyncResult result)
    {
        Accept_connection();
        TcpClient client = server.EndAcceptTcpClient(result);

        NetworkStream ns = client.GetStream();

        while (client.Connected)
        {
            byte[] size = new byte[8];
            ns.Read(size, 0, size.Length);
            string dataSizeStr = Encoding.UTF8.GetString(size, 0, size.Length);

            int.TryParse(dataSizeStr, out int dataSize);

            byte[] msg = new byte[dataSize]; 
            try
            {
                ns.Read(msg, 0, msg.Length)
                var pk = PKMConverter.GetPKMfromBytes(msg);
                var lc = new LegalityCheck(pk);
                byte[] report = Encoding.Default.GetBytes(lc.Report);
                ns.Write(report, 0, report.Length);
                ns.Flush();
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
