using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using om.utils;
using System.IO;

namespace Test
{
    [Option("hi", tip = "hi, find neighbors!")]
    class HiCmd:CmdLine.ICmd
    {
        [Option("p",tip = "port, default is 7788")]
        public int port = 7788;
        [Option("name",tip= "only the {name} one replay, default is null,all replay")]
        public string name;
        [Option("server",alias = "s", tip = "is server mode, default is false")]
        public bool server_mode = false;

        public void Exec()
        {
            if (server_mode)
            {
                RunServerLoop();
            }
            else
            {
                RunClientLoop();
            }
        }

        void RunClientLoop()
        {
            MsgAsk.AskServer(name, port);
            
            Console.WriteLine("Press Esc To Exit, Enter To ReAsk");
            while (true)
            {
                var line = Console.ReadKey();
                if (line.Key == ConsoleKey.Escape)
                {
                    return;
                }
                else if (line.Key == ConsoleKey.Enter)
                {
                    MsgAsk.AskServer(name, port);
                }
            }
        }

        void RunServerLoop()
        {
            HiServer.singleton.BeginWork(name, port);
            Console.WriteLine("Press Esc To Exit");
            while (true)
            {
                var line = Console.ReadKey();
                if(line.Key == ConsoleKey.Escape)
                {
                    return;
                }
            }
        }
    }

    public class ServerState
    {
        public UdpClient sock;
        public IPEndPoint ip;
    }

    public class MsgAsk
    {
        string name;

        public static void AskServer(string name,int port)
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, port);
            UdpClient sock = new UdpClient();

            MsgAsk ask = new MsgAsk();
            ask.name = name;

            var bytes = HiMgr.serializer.Serialize(ask);
            sock.Send(bytes, bytes.Length, ip);
            Console.WriteLine("Wait For Replay");

            ServerState state = new ServerState();
            state.ip = ip;
            state.sock = sock;
            sock.BeginReceive(EndReceive, state);
        }

        static void EndReceive(IAsyncResult ar)
        {
            try
            {
                ServerState state = (ServerState)ar.AsyncState;
                var bytes = state.sock.EndReceive(ar, ref state.ip);
                MsgAns ask = HiMgr.serializer.Deserialize<MsgAns>(bytes);
                ask.OnRecv();
            }
            catch (Exception e)
            {
                Console.WriteLine($"client recv error, {e.Message}");
            }
        }

        public void OnRecv(ServerState state)
        {
            if (string.IsNullOrWhiteSpace(name) || HiServer.name == name)
            {
                MsgAns.ReplayClient(state);
            }
        }
    }

    public class MsgAns
    {
        string info;

        public void OnRecv()
        {
            Console.WriteLine($"client recv: {info}");
        }

        public static void ReplayClient(ServerState state)
        {
            MsgAns ans = new MsgAns();
            ans.info = $"我的地址是{HiServer.IP}，名字是{HiServer.name}";
            var bytes = HiMgr.serializer.Serialize(ans);
            Console.WriteLine($"rev a ask from {state.ip}");
            state.sock.Send(bytes, bytes.Length, state.ip);
        }
    }

    static class HiMgr
    {
        public static XSerialize.XBinarySerializer serializer;
        public static void Init()
        {
            serializer = new XSerialize.XBinarySerializer(typeof(MsgAsk),typeof(MsgAns));
        }
    }

    class HiServer
    {
        public static HiServer singleton = new HiServer();

        public static string IP = "0.0.0.0";
        public static string name = null;

        public static string GetLocalIP()
        {
            try
            {
                string HostName = Dns.GetHostName(); //得到主机名
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        return IpEntry.AddressList[i].ToString();
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public void BeginWork(string name, int port)
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);
            UdpClient sock = new UdpClient(ip);
            ServerState state = new ServerState();
            state.ip = ip;
            state.sock = sock;
            HiServer.name = name;
            IP = GetLocalIP();
            Console.WriteLine($"Server Start:{IP}");
            sock.BeginReceive(EndReceive, state);
        }

        void EndReceive(IAsyncResult ar)
        {
            ServerState state = (ServerState)ar.AsyncState;
            try
            {
                var bytes = state.sock.EndReceive(ar, ref state.ip);
                MsgAsk ask = HiMgr.serializer.Deserialize<MsgAsk>(bytes);
                ask.OnRecv(state);
            }
            catch(Exception e)
            {
                Console.WriteLine($"server recv error, {e.Message}");
            }
            finally
            {
                state.sock.BeginReceive(EndReceive, state);
            }
        }
    }

    class Program
    {
        public static bool exit = false;
        static void Main(string[] args)
        {
            HiMgr.Init();
            var cmd_parser = CmdLine.CreateCmdParser<HiCmd>();
            cmd_parser.Parse(args)?.Exec();
        }
    }
}
