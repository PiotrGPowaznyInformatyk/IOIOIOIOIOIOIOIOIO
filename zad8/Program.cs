using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace zad8
{
    class Program
    {
        delegate int DelegateType(int a);
        static DelegateType delegatSilniaRekurencyjna, delegatSilniaIteracyjna, delegatIterFibonacci, delegatRekuFibonacci;

        #region Fibo+Silnie
        static int SilniaRekurencyjna(int number)
        {
            if (number < 2)
                return 1;
            return number * SilniaRekurencyjna(number - 1);
        }

        static int SilniaIteracyjna(int number)
        {
            var nmbr = number;
            for (int i = nmbr - 1; i > 1; i--)
            {
                nmbr = nmbr * i;
            }
            return nmbr;
        }
        static int IterFibonacci(int number)
        {
            int x = 0, y = 1, z = 1;
            for (int i = 0; i < number; i++)
            {
                x = y;
                y = z;
                z = x + y;
            }
            return x;
        }
        static int RekuFibonacci(int number)
        {
            if (number == 0) return 0;
            if (number == 1) return 1;
            else return RekuFibonacci(number - 1) + RekuFibonacci(number - 2);
        }
        #endregion


        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            IAsyncResult ar;
            int nmbr = 20;
            delegatSilniaRekurencyjna = new DelegateType(SilniaRekurencyjna);
            delegatSilniaIteracyjna = new DelegateType(SilniaIteracyjna);
            delegatIterFibonacci = new DelegateType(IterFibonacci);
            delegatRekuFibonacci = new DelegateType(RekuFibonacci);

            DelegateType[] variousDelegates = { delegatSilniaRekurencyjna, delegatSilniaIteracyjna, delegatIterFibonacci, delegatRekuFibonacci};

            //niestety w ponizszej implementacji nie korzystamy w pelni z wspolbieznosci 
            foreach(var delegat in variousDelegates)
            {
                sw.Reset();
                sw.Start();
                ar = delegat.BeginInvoke(nmbr, null, null);
                int result = delegat.EndInvoke(ar);
                sw.Stop();

                Console.WriteLine("{0} zwrocil wynik: {1}, \npo czasie \t\t{2} ticków.\n", delegat.Method.Name, result, sw.ElapsedTicks);
            }
            Console.WriteLine("Press any key to quit.");    
            Console.ReadKey();
        }
    }
}
