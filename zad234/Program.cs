using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace zad234
{
    /*
    Zad 2
    problemem, z ktorym spotkalismy sie w zadaniu drugim jest blokowanie sie serwera na innych klientow, kiedy rozmawia z pierwszym polaczeniem.
    Aby tego uniknac, nalezy umiescic zadania wykonywane przez serwer w nowym watku.

    Zad3
    Problem pojawiajacy sie w tym zadaniu wiaze sie z procesem wywlaszczania i zmiany kontekstu - watki sa wywlaszczane zanim zdaza wyslac wiadomosc
    w odpowiednim kolorze - w kilku iteracjach programu wyraznie widac duza losowosc w pojawianiu sie tego bledu. Nie ma reguly, poniewaz planista
    raz uzna, ze watek numer jeden jest wazniejszy niz watek numer dwa, a drugi raz na odwrot.
    
    Zad4
    Uzywajac locka upewniamy sie, ze watek nie zostanie wywlaszczony w trakcie wykonywania kodu 'wrazliwego', jakim jest kolorowanie swojej wypowiedzi
    w konsoli.
    */
    class Program
    {
        private static Object thisLock = new Object();
        private static int SleepTime = 1;
        static void writeConsoleMessage(string message, ConsoleColor color)
        {
            lock (thisLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            //lock musi być globalny (umieszczony poziom wyzej niz w wykonywanej funkcji), poniewaz nie chcemy znalezc sie w sytuacji, 
            //w ktorej kazdy watek tworzy wlasny lock, ktorego nie widzi reszta wykonywanego programu
        }
        static void Main(string[] args)
        {
            //start up server
            ThreadPool.QueueUserWorkItem(ServerThread);

            //start up fake clients asking server for echo
            ThreadPool.QueueUserWorkItem(ClientProc, new object[] { "Pierwsza informacja", 300 });
            ThreadPool.QueueUserWorkItem(ClientProc, new object[] { "JednoczesnieA", 2000 });
            ThreadPool.QueueUserWorkItem(ClientProc, new object[] { "testtest", 1200 });
            ThreadPool.QueueUserWorkItem(ClientProc, new object[] { "JednoczesnieB", 2000 });


            Console.ReadKey();
        }


        static void ServerThread(Object stateInfo)
        {
            TcpListener server = new TcpListener(IPAddress.Any, 2048);
            server.Start();

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                //dodajemy nowy watek "podwykonawce" - w ten sposob serwer nie zawiesza sie i od razu nasluchuje polaczenia od kolejnego klienta
                ThreadPool.QueueUserWorkItem(SubServerThread, new object[] { client });
            }
        }


        static void SubServerThread(Object stateInfo)
        {
            byte[] buffer = new byte[1024];
            TcpClient client = (TcpClient)((object[])stateInfo)[0];

            int len = client.GetStream().Read(buffer, 0, 1024);
            client.GetStream().Write(buffer, 0, buffer.Length);
            string wiad = new ASCIIEncoding().GetString(buffer, 0, len);
            //
            writeConsoleMessage("Server answers: " + wiad, ConsoleColor.DarkRed);
            client.Close();
        }


        static void ClientProc(Object stateInfo)
        {
            TcpClient client = new TcpClient();
            var data1 = ((object[])stateInfo)[0];
            string msg = (string)data1;
            data1 = ((object[])stateInfo)[1];
            int waitTime = (int)data1;

            client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2048));
            Thread.Sleep(waitTime);
            byte[] message = new ASCIIEncoding().GetBytes(msg);

            writeConsoleMessage("Client Thread " + Thread.CurrentThread.ManagedThreadId + " sends: " + msg, ConsoleColor.DarkGreen);

            client.GetStream().Write(message, 0, message.Length);

            //give up rest of time slice
            Thread.Sleep(SleepTime);


            NetworkStream stream = client.GetStream();
            stream.Read(message, 0, message.Length);
            msg = new ASCIIEncoding().GetString(message);

            writeConsoleMessage("Client Thread " + Thread.CurrentThread.ManagedThreadId + " reads: " + msg, ConsoleColor.DarkGreen);

        }
    }
}
