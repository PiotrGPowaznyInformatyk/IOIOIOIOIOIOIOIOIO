using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace zad5
{
    /*
    niestety nie udalo mi sie uzyskac pozadanego efektu - celem zadania bylo udowodnienie, ze wielowatkowosc nie zawsze jest oplacalna.
    Tymczasem, otrzymuje srednie czasy: ok. 300ms dla jednego watku, oraz ~60 dla wielu.
    */
    class Program
    {
        static WaitHandle[] waitHandles;
        static EventWaitHandle canStart;
        static Stopwatch sw = new Stopwatch();
        static int rozmiarTablicy = 64000000;
        static int[] tablica = new int[rozmiarTablicy];
        static long wynikWedlugWatkow = 0;
        static void Main(string[] args)
        {
            Console.WriteLine("Loading...\n");


            Random rng = new Random();
            //ile wynosi faktyczna suma elementow w tablicy
            long suma = 0;
            int ileWatkowMAX = 64;
            int ileWatkowIstnieje = 0;
            long howLongSingleThread = 0;
            //tworzymy losowa tablice intow
            for (int i = 0; i < rozmiarTablicy; i++)
            {
                int x = rng.Next(0, 500);
                tablica[i] = x;
            }
            Console.WriteLine("Created random table...");

            //sumujemy elementy w jednym watku
            sw.Reset();
            sw.Start();
            for (int i = 0; i < rozmiarTablicy; i++)
            {
                suma += tablica[i];
            }
            sw.Stop();
            howLongSingleThread = sw.ElapsedMilliseconds;
            sw.Reset();
            Console.WriteLine("Summed it up in one thread...");

            //pętla przydzielająca kawałki tablicy wątkom
            List<int> poczatekSumowania = new List<int>();
            List<int> koniecSumowania = new List<int>();
            for (int i = 0; i < rozmiarTablicy;)
            {
                ileWatkowIstnieje++;

                if (ileWatkowIstnieje == 1)
                {
                    //pierwszy watek
                    poczatekSumowania.Add(i);
                }
                else
                {
                    //kolejne watki
                    poczatekSumowania.Add(i + 1);
                }

                int x = (rozmiarTablicy / ileWatkowMAX);
                i += x;

                if (i >= rozmiarTablicy)
                {
                    //opuszczamy petle - ostatni watek
                    koniecSumowania.Add(rozmiarTablicy - 1);
                }
                else
                {
                    koniecSumowania.Add(i);
                }
            }
            Console.WriteLine("Created two lists for threads...");

            //create waithandles
            waitHandles = new WaitHandle[ileWatkowIstnieje];
            for (int i = 0; i < ileWatkowIstnieje; i++)
            {
                AutoResetEvent handle = new AutoResetEvent(false);
                waitHandles[i] = handle;
            }
            canStart = new EventWaitHandle(false, EventResetMode.ManualReset);
            Console.WriteLine("Created waithandles...");

            //create pools
            Object thisLock = new Object();


            Console.WriteLine("\nPress any key to begin.");
            Console.ReadKey();
            Console.WriteLine("Starting: ");

            lock (thisLock)
            {
                for (int i = 0; i < ileWatkowIstnieje; i++)
                {
                    ThreadPool.QueueUserWorkItem(ThreadProc, new object[] { poczatekSumowania[i], koniecSumowania[i], waitHandles[i], canStart });
                }
            }


            //set timer for many threads
            sw.Reset();
            sw.Start();
            //tell threads they can start adding
            canStart.Set();
            //wait for all threads to finish
            WaitHandle.WaitAll(waitHandles);
            sw.Stop();


            Console.WriteLine("Watkow w systemie: \t{0}.\nSuma: \t\t\t\t{1}.\nSuma obliczona przez watki: \t{2}.", ileWatkowIstnieje, suma,
                wynikWedlugWatkow);
            Console.WriteLine("Czas jednego watku: {0} ms\nCzas wielu watkow: {1} ms", howLongSingleThread, sw.ElapsedMilliseconds);
            Console.ReadKey();
        }



        //metody uzywane przez watki
        static int sumowanie(int poczatek, int koniec)
        {
            int wynik = 0;
            for (int i = poczatek; i <= koniec; i++)
            {
                wynik += tablica[i];
            }
            return wynik;
        }

        static void dodajWynik(int ile)
        {
            wynikWedlugWatkow += ile;
        }

        // This thread procedure performs the task.
        static void ThreadProc(Object stateInfo)
        {
            var data = ((object[])stateInfo)[3];
            EventWaitHandle canStart = (EventWaitHandle)data;
            //wait for main to signal start
            canStart.WaitOne();

            data = ((object[])stateInfo)[0];
            int poczatek = (int)data;

            data = ((object[])stateInfo)[1];
            int koniec = (int)data;

            data = ((object[])stateInfo)[2];
            AutoResetEvent handle = (AutoResetEvent)data;

            int wynik = sumowanie(poczatek, koniec);
            dodajWynik(wynik);

            handle.Set();
        }
    }
}

