using System;
using System.Threading;

namespace zad1
{
    class Program
    {
        static void Main(string[] args)
        {
            ThreadPool.QueueUserWorkItem(ThreadProc, new object[] { 1200 });
            ThreadPool.QueueUserWorkItem(ThreadProc, new object[] { 1000 });

            Console.WriteLine("Main.");
            Console.ReadKey();
        }

        // This thread procedure performs the task.
        static void ThreadProc(Object stateInfo)
        {
            //cast data from stateInfo to integer
            var data = ((object[])stateInfo)[0];
            int integer = (int)data;
            Thread.Sleep(integer);
            //wypisujemy w konsoli ile czekal dany proces
            Console.WriteLine("{0} waited: {1}", Thread.CurrentThread.ManagedThreadId, integer);
        }
    }
}
