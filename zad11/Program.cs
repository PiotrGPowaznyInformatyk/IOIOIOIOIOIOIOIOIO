using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace zad11
{
    class Program
    {
        private static Object thisLock = new Object();
        public static BackgroundWorker bgw;
        //sleepTime = 1 for clientthread to give up his remaining time slice
        public static int SleepTime = 1;
        //time client waits after connecting before starting to send a msg
        public static int waitTime = 500;
        //handle server sets
        public static WaitHandle handle = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            bgw = new BackgroundWorker();
            int numer = 10;
            int serverNmb = 0;
            bool running = false;
            InitializeBackgroundWorker();

            Console.WriteLine("'s' start server (bg worker)");
            Console.WriteLine("'c' cancel server (bg worker)");
            Console.WriteLine("'k' start a new client thread");
            Console.WriteLine("'d' dispose and quit");

            char input = 'A';
            while (input != 'q')
            {
                input = Console.ReadKey().KeyChar;
                Console.WriteLine();
                switch (input)
                {
                    case 's':
                        serverNmb++;
                        if (!running)
                        {
                            bgw.RunWorkerAsync(serverNmb);
                            running = true;
                        }
                        else
                            writeConsoleMessage(@"More than one worker not supported in ProfessionalBGWorkerDemoApplication
                                                \nFor full version donate 10 BTC to Piotr Grabowski", ConsoleColor.Yellow);
                        break;
                    case 'k':
                        ThreadPool.QueueUserWorkItem(ClientThread, new object[] { numer });
                        numer++;
                        break;
                    case 'c':
                        if (running)
                        {
                            running = false;
                            bgw.CancelAsync();
                        }else
                            writeConsoleMessage(@"There is nothing to cancel.", ConsoleColor.Yellow);
                        break;
                    case 'd':
                        running = false;
                        bgw.Dispose();
                        break;
                    case 'q':
                        break;
                }
            }
        }



        #region WorkerRegion
        public static void InitializeBackgroundWorker()
        {
            bgw.DoWork +=
                new DoWorkEventHandler(backgroundWorker_DoWork);
            bgw.RunWorkerCompleted +=
                new RunWorkerCompletedEventHandler(
            backgroundWorker_RunWorkerCompleted);
            bgw.ProgressChanged +=
                new ProgressChangedEventHandler(
            backgroundWorker_ProgressChanged);
            bgw.Disposed += new EventHandler(backgroundWorker_Disposed);

            bgw.WorkerSupportsCancellation = true;
            bgw.WorkerReportsProgress = true;
        }
        static private void backgroundWorker_ProgressChanged(object sender,
           ProgressChangedEventArgs e)
        {
            writeConsoleMessage("Server accepted "+e.ProgressPercentage.ToString()+" clients so far", ConsoleColor.DarkRed);
        }
        static private void backgroundWorker_Disposed(object sender,
            EventArgs e)
        {
            bgw.CancelAsync();
            writeConsoleMessage("Server disposed!", ConsoleColor.DarkRed);
        }
        static private void backgroundWorker_DoWork(object sender,
            DoWorkEventArgs e)
        {
            int howMany = (int)e.Argument;
            switch (howMany)
            {
                case 1:
                    writeConsoleMessage(howMany+"st time the server is starting up...", ConsoleColor.Red);
                    break;
                case 2:
                    writeConsoleMessage(howMany+"nd time the server is starting up...", ConsoleColor.Red);
                    break;
                case 3:
                    writeConsoleMessage(howMany + "rd time the server is starting up...", ConsoleColor.Red);
                    break;
                default:
                    writeConsoleMessage(howMany + "th time the server is starting up...", ConsoleColor.Red);
                    break;
            }

            BackgroundWorker worker = sender as BackgroundWorker;
            int acceptedCount = 0;
            TcpListener server = new TcpListener(IPAddress.Any, 2048);
            server.Start();
            while (true)
            {
                if (worker.CancellationPending)
                {
                    //e.Cancel =true
                    //powyzsze pole jest zakomentowane, poniewaz serwer w tej aplikacji nie moze inaczej skonczyc pracy niz na zyczenie uzytkownika
                    //a ustawenie cancel na true sprawi, ze w metodzie "runworkerCompleted" nie bedziemy mogli dostac sie do pola Result
                    //jest to troche nieintuicyjne rozwiazanie z mojej strony - bo cancelAsync defacto spelnia funkcje zwyklego wylacznika,
                    //poniewaz bgworker nie moze sie inaczej skonczyc (jest w nieskonczonej petli, to ma byc serwer echo)

                    break;
                }
                if (!server.Pending())
                {
                    Thread.Sleep(500); // choose a number (in milliseconds) that makes sense
                    continue; // skip to next iteration of loop
                    //dzieki temu ifowi, nasz serwer moze sie elegancko wylaczyc kiedy cancelujemy backgroundworkera
                }

                TcpClient client = server.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(SubServerThread, new object[] { client, worker });

                acceptedCount++;
                bgw.ReportProgress(acceptedCount);
            }
            server.Stop();
            e.Result = acceptedCount;
        }
            static private void backgroundWorker_RunWorkerCompleted(
            object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                writeConsoleMessage("Canceled", ConsoleColor.Red);
            }
            else
            {
                writeConsoleMessage("Server accepted "+e.Result.ToString()+" clients before ending its work.", ConsoleColor.Red);
            }

        }
        #endregion


        #region ThreadMethods+writeConsoleMsg
        static void writeConsoleMessage(string message, ConsoleColor color)
        {
            lock (thisLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        static void SubServerThread(Object stateInfo)
        {
            byte[] buffer = new byte[1024];
            TcpClient client = (TcpClient)((object[])stateInfo)[0];
            BackgroundWorker worker = (BackgroundWorker)((object[])stateInfo)[1];

            int len = client.GetStream().Read(buffer, 0, 1024);
            client.GetStream().Write(buffer, 0, buffer.Length);
            string wiad = new ASCIIEncoding().GetString(buffer, 0, len);
            writeConsoleMessage("Server answers: " + wiad, ConsoleColor.DarkRed);
            client.Close();
        }

        static void ClientThread(Object stateInfo)
        {
            TcpClient client = new TcpClient();
            var data1 = ((object[])stateInfo)[0];
            string msg = (string)data1.ToString();

            client.ConnectAsync(IPAddress.Parse("127.0.0.1"), 2048).Wait(1000);
            Thread.Sleep(waitTime);
            byte[] message = new ASCIIEncoding().GetBytes(msg);

            if (client.Connected)
            {
                writeConsoleMessage("Client Thread " + Thread.CurrentThread.ManagedThreadId + " sends: " + msg, ConsoleColor.DarkGreen);

                client.GetStream().Write(message, 0, message.Length);

                //give up rest of time slice
                Thread.Sleep(SleepTime);


                NetworkStream stream = client.GetStream();
                stream.Read(message, 0, message.Length);
                msg = new ASCIIEncoding().GetString(message);

                writeConsoleMessage("Client Thread " + Thread.CurrentThread.ManagedThreadId + " reads: " + msg, ConsoleColor.DarkGreen);
            }
            else writeConsoleMessage("Client Thread " + Thread.CurrentThread.ManagedThreadId + " failed to connect!", ConsoleColor.Green);



        }
        #endregion
    }
}
