using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace zad7
{
    class Program
    {
        static void Main(string[] args)
        {
            String fileName = "tekst.txt";

            FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[file.Length];
            WaitHandle handle = new AutoResetEvent(false);

            IAsyncResult iar = file.BeginRead(buffer, 0, (int)file.Length, null, null);

            //w tym miejscu watek sie zatrzymuje, i czeka, az asynchroniczny watek wywolany w file.BeginRead skonczy prace.
            //dzieki takiemu rozwiazaniu kod wyglada jakby byl jednowatkowy - latwo sie go programuje, a zarazem posiada szybkosc wielowatkowosci
            file.EndRead(iar);

            Console.WriteLine(Encoding.ASCII.GetString(buffer));

            Console.WriteLine("Finished executing main thread. Press any key to continue...");
            Console.ReadKey();
        }
    }
}
