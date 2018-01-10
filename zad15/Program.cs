using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


/*
w tym projekcie znajduja sie dwa zadania
pierwsze zadanie, ktore wyswietla sie w logach to 

*/
namespace zad15
{
    enum PACKET_TYPE
    {
        HI,
        ACK,
        BYE
    }

    #region ZADANIE DODATKOWE
    class Eter
    {
        //space in which our servers and clients exist
        //assumption: everyone sees everyone in eter


        #region Properties
        private List<Server> servers;
        private List<Client> clients;

        public List<Server> Servers
        {
            get { return servers; }
        }
        public List<Client> Clients
        {
            get { return clients; }
        }
        #endregion

        #region Constructor
        public Eter()
        {
            this.servers = new List<Server>();
            this.clients = new List<Client>();
        }
        #endregion

        #region Methods
        public void Add(Client client)
        {
            this.clients.Add(client);
        }
        public void Add(Server server)
        {
            this.servers.Add(server);
        }
        public void Remove(Client client)
        {
            this.clients.Remove(client);
        }
        public void Remove(Server server)
        {
            this.servers.Remove(server);
        }
        #endregion

    }

    class Logger
    {
        private static Object thisLock = new Object();
        private bool writeToConsole = true;
        private static int iter = 0;
        public string filename="";


        #region Constructors
        public Logger(bool writeToConsole)
        {
            this.writeToConsole = writeToConsole;
            filename = "";
        }
        public Logger(bool writeToConsole, string filename)
        {
            this.writeToConsole = writeToConsole;
            this.filename = filename;
        }
        #endregion

        #region Methods
        public bool toConsole() { return writeToConsole; }
        public void writeLog(string message, ConsoleColor color)
        {
            if (toConsole())
            {
                lock (thisLock)
                {
                    Console.ForegroundColor = color;
                    Console.WriteLine(message);
                    Console.ResetColor();
                }
            }
            else
            {
                //to file
                lock (thisLock)
                {
                    Console.WriteLine("Writing to file ("+iter+")");
                    iter++;
                    File.AppendAllText(filename, message+'\r'+'\n');
                }
            }
        }
        #endregion
    }

    class Packet
    {
        private PACKET_TYPE packet_type;
        public PACKET_TYPE Packet_type
        {
            get { return packet_type; }
        }
        public Packet(PACKET_TYPE packet_type)
        {
            this.packet_type = packet_type;
        }
    }
    #endregion


    class Server
    {
        #region Variables
        //eter
        private Eter eter;
        //logger
        Logger log;
        //server
        private TcpListener server;
        //ipaddresses to listen to
        private IPAddress address;
        //what port to listen to
        private int port;
        private int id;
        //is it running?
        private static bool running = false;

        private CancellationTokenSource cts = new CancellationTokenSource();
        private Task serverTask;
        #endregion

        #region Properties
        public IPAddress Address
        {
            get { return address; }
            set
            {
                //server can't be running if you want to change that
                if (!running)
                    address = value;
                else log.writeLog("Server can't be running to change address.", ConsoleColor.Yellow);
            }
        }
        public int Port
        {
            get { return port; }
            set
            {
                //server can't be running if you want to change that
                if (!running)
                    port = value;
                else log.writeLog("Server can't be running to change port.",ConsoleColor.Yellow);

            }
        }
        public Task ServerTask
        {
            get { return serverTask; }
        }
        public int Id
        {
            get { return id; }
            set { this.id = value; }
        }
        #endregion

        #region Constructors
        public Server(Logger log, Eter eter, int id)
        {
            this.address = IPAddress.Any;
            this.port = 2048;
            this.server = new TcpListener(address, port);
            this.log = log;
            this.eter = eter;
            this.id = id;
        }

        #endregion

        #region Methods
        #region ZADANIE DODATKOWE METODY
        public Packet TalkBack(Packet packet, Client c)
        {
            switch (packet.Packet_type)
            {
                case PACKET_TYPE.HI:
                    log.writeLog("Server send HI to " + c.Id, ConsoleColor.DarkCyan);
                    break;
                case PACKET_TYPE.ACK:
                    log.writeLog("Server send ACK to " + c.Id, ConsoleColor.DarkCyan);
                    break;
                case PACKET_TYPE.BYE:
                    log.writeLog("Server send BYE to " + c.Id, ConsoleColor.DarkCyan);
                    break;
            }
            return packet;
        }
        #endregion

        public void Start()
        {
            serverTask = RunAsync(cts.Token);
        }
        public void Stop()
        {
            CancelRq();
        }
        private void CancelRq()
        {
            cts.Cancel();
            log.writeLog("Serwer konczy galop.",ConsoleColor.Red);
            server.Stop();
        }
        private async Task RunAsync(CancellationToken cts)
        {
            try
            {
                server.Start();
                running = true;
                log.writeLog("Server started!", ConsoleColor.Red);
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            while (true && !cts.IsCancellationRequested)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                byte[] buffer = new byte[1024];
                log.writeLog("Server found someone!", ConsoleColor.Red);

                using (cts.Register(() => client.GetStream().Close()))
                {
                    client.GetStream().ReadAsync(buffer, 0, buffer.Length, cts).ContinueWith(
                        async (t) =>
                        {
                            int i = t.Result;
                            while (true)
                            {
                                //lets wait a lil'
                                Thread.Sleep(200);

                                log.writeLog("Server answers!", ConsoleColor.Red);

                                client.GetStream().WriteAsync(buffer, 0, i, cts);
                                try
                                {
                                    i = await client.GetStream().ReadAsync(buffer, 0, buffer.Length, cts);
                                }
                                catch
                                {
                                    break;
                                }
                            }
                        });
                }
            }

        }
        #endregion
    }

    class Client
    {
        #region variables
        Logger log;
        TcpClient client;
        private int id;
        private int waitInterval;
        private Eter eter;
        private List<Server> knownServers;
        private CancellationTokenSource cts;

        #endregion
        #region properties

        public int Id
        {
            get { return id; }
            set { this.id = value; }
        }
        #endregion
        #region Constructors
        public Client(int id, int waitInterval, Logger log, Eter eter)
        {
            this.client = new TcpClient();
            this.knownServers = new List<Server>();
            cts = new CancellationTokenSource();
            this.id = id;
            this.waitInterval = waitInterval;
            this.log = log;
            this.eter = eter;
        }
        #endregion
        #region Methods
        #region ZADANIE DODATKOWE METODY
        public bool TWH(Server s)
        {
            bool success = false;
            Client c = this;

            log.writeLog("Client " + c.Id + " send HI to someone.", ConsoleColor.Yellow);
            if (c.Discover(s))
            {
                //discovered
                log.writeLog("Client " + c.Id + " send ACK to someone.", ConsoleColor.Yellow);
                if (c.Communicate(PACKET_TYPE.ACK))
                {
                    success = true;
                }
                else
                {
                    log.writeLog("Client " + c.Id + " failed to be ACKed back.", ConsoleColor.Yellow);
                }
                
            }
            else log.writeLog("Client " + c.Id + " failed be HI'd back.", ConsoleColor.Yellow);

            return success;
        }
        public bool Discover(Server s)
        {
            bool foundSomeone = false;
            //lets see if i can see anyone in the eter
            foreach(Server serv in eter.Servers)
            {
                //oh, here's the guy you wanted
                if(serv == s)
                {
                    if (s.TalkBack(new Packet(PACKET_TYPE.HI), this).Packet_type == PACKET_TYPE.HI)
                    {
                        this.knownServers.Add(s);
                        foundSomeone = true;
                    }
                }
            }
            return foundSomeone;
        }
        public bool Communicate(PACKET_TYPE packet_type)
        {
            bool ackedSomeone = false;
            foreach (Server s in this.knownServers)
            {
                if (s.TalkBack(new Packet(PACKET_TYPE.ACK), this).Packet_type == PACKET_TYPE.ACK)
                {
                    //do ack stuff

                    ackedSomeone = true;
                }
            }

            return ackedSomeone;
        }
        public bool Bye(Server s)
        {
            bool byed = false;
            if (knownServers.Contains(s))
            {
                if (s.TalkBack(new Packet(PACKET_TYPE.BYE), this).Packet_type == PACKET_TYPE.BYE)
                {
                    knownServers.Remove(s);
                    byed = true;
                }
            }
            return byed;
        }
        public void ByeAll()
        {
            foreach (Server s in knownServers)
            {
                s.TalkBack(new Packet(PACKET_TYPE.BYE), this);
                log.writeLog("Klient "+ Id + " byed server(id:" + s.Id+")", ConsoleColor.DarkYellow);
            }
        }
        #endregion


        public void Connect()
        {
            client.ConnectAsync(IPAddress.Parse("127.0.0.1"), 2048);
            log.writeLog("Klient " + Id + " connected successfully.", ConsoleColor.DarkGreen);
        }
        private async Task<string> Ping(string message)
        {
            byte[] buffer = new ASCIIEncoding().GetBytes(message);
            await client.GetStream().WriteAsync(buffer, 0, buffer.Length);
            log.writeLog("Klient " + Id + " pings.", ConsoleColor.Cyan);

            buffer = new byte[1024];
            var t = await client.GetStream().ReadAsync(buffer, 0, buffer.Length);

            log.writeLog("Klient " + Id + " reads: "+ Encoding.ASCII.GetString(buffer, 0, t), ConsoleColor.DarkCyan);

            return Encoding.ASCII.GetString(buffer, 0, t);
        }

        public async Task<int> keepPinging(string message)
        {
            List<string> messages = new List<string>();
            int pingCount = 0;
            bool done = false;
            while (!done)
            {
                //obsluga tokenu
                if (cts.IsCancellationRequested)
                    done = true;


                await Ping(message);
                pingCount++;
                //zwolnij ten galop
                Thread.Sleep(waitInterval);
            }
            log.writeLog("Klient " + Id + " is cancelling", ConsoleColor.DarkYellow);
            return pingCount;
        }

        public void Cancel()
        {
            cts.Cancel();
        }
        public void CancelAfter(int delay)
        {
            cts.CancelAfter(delay);
        }
        #endregion
    }




    class Program
    {
        static void Main(string[] args)
        {
            Eter eter = new Eter();
            Logger log = new Logger(true);
            string filename = "logi.txt";
            //Logger log = new Logger(false, filename);

            //wipe filename.txt file
            File.WriteAllText(filename, "");


            Server s = new Server(log, eter, 1);

            int howManyClients = 3;
            Client[] clients = new Client[howManyClients];

            clients[0] = new Client(1, 500, log, eter);
            clients[1] = new Client(2, 1000, log, eter);
            clients[2] = new Client(3, 1200, log, eter);
            eter.Add(s);
            foreach(Client c in clients)
            {
                eter.Add(c);
            }


            foreach(Client c in clients)
            {
                if (c.TWH(s))
                {
                    log.writeLog(c.Id + " managed to connect to someone.", ConsoleColor.DarkYellow);
                }
                else log.writeLog(c.Id + " failed to connect to someone.", ConsoleColor.DarkYellow);

            }
            Console.WriteLine("Clients have tried discovering. \nProceed? (If clients failed, it's dangerous to proceed!)");
            log.writeLog("DRUGA CZESC ZADANIA - KOMUNIKACJA NA WATKACH", ConsoleColor.White);
            Console.ReadKey();
            foreach(Client c in clients)
            {
                c.Communicate(PACKET_TYPE.ACK);
            }

            s.Start();


            foreach(Client c in clients)
            {
                c.Connect();
            }

            clients[0].CancelAfter(2000);
            clients[1].CancelAfter(3500);
            clients[2].CancelAfter(6000);


            List<Task> taski = new List<Task>();
            foreach (Client c in clients)
            {
                taski.Add(c.keepPinging(c.Id + " pings."));
            }

            Task.WaitAll(taski.ToArray());
            log.writeLog("Main is done waiting.", ConsoleColor.Yellow);
            foreach(Client c in clients)
            {
                c.ByeAll();
            }
            s.Stop();

            Console.WriteLine("DONE, press any key to quit.\n{0}, {1}, {2}", ((Task<int>)taski[0]).Result, ((Task<int>)taski[1]).Result, ((Task<int>)taski[2]).Result);
            Console.ReadKey();
        }
    }
}
