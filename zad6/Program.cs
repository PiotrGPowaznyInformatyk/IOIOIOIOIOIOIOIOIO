using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace lab2
{
    class Program
    {
        static void Main(string[] args)
        {
            String fileName = "tekst.txt";

            FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[file.Length];
            WaitHandle handle = new AutoResetEvent(false);

            file.BeginRead(buffer, 0, (int)file.Length, AsyncCallback, new object[] { file, buffer, handle });

            handle.WaitOne();

            Console.WriteLine("Finished executing main thread. Press any key to continue...");
            Console.ReadKey();
        }

        static void AsyncCallback(IAsyncResult state)
        {
            var tmp = (object[])state.AsyncState;
            
            FileStream file = (FileStream)tmp[0];
            byte[] buffer = (byte[])tmp[1];
            AutoResetEvent handle = (AutoResetEvent)tmp[2];

            Console.WriteLine(ASCIIEncoding.ASCII.GetString(buffer));
            file.EndRead(state);
            handle.Set();
        }
    }
}
