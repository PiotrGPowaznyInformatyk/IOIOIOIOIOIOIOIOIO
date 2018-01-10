using System;
using System.Threading;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Collections;

/*
sources of knowledge
   1. prezentacja od prowadzącego
   2. https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/component-that-supports-the-event-based-asynchronous-pattern
*/

namespace zad9
{
    class Program
    {
        //po kazdym obliczeniu kolejnego 'oczka' macierzy wynikowej wywolujemy funkcje "thread.sleep(sleepTime)", aby program nie dzialal zbyt
        //szybko, i moznabylo np. odwolac niektore watki w trakcie pracy
        public static int sleepTime = 100;
        //

        static void Main(string[] args)
        {
            MatMulCalculator kalkulatorMnMacierzy = new MatMulCalculator();
            const int size = 4;

            double[,] mat1 = new double [size, size]
            {
                {5,2,6,1 },
                {0,6,2,0 },
                {3,8,1,4 },
                {1,8,5,6 }
            };
            double[,] mat2 = new double [size, size]
            {
                {7,5,8,0 },
                {1,8,2,6 },
                {9,4,3,8 },
                {5,3,7,9 }
            };
            /*
            poprawny wynik:
            96 68 69 69 
            24 56 18 52
            58 95 71 92
            90 107 81 142
            */
            int id = 1;
            char input = 'A';
            Console.WriteLine("'q' to quit.\n'n' to start new thread.\n'c' to cancel a thread.\n'p' to print list of current threads.");

            while (input != 'q')
            {

                input = Console.ReadKey().KeyChar;

                switch (input)
                {
                    case 'n':
                        Console.WriteLine("\nStarting new working thread, id: {0}", id);
                        kalkulatorMnMacierzy.MatMulAsync(mat1, mat2, size, id);
                        id++;
                        break;
                    case 'c':
                        Console.WriteLine("\nCancel who? (pass id)");
                        int deleteiD = int.Parse(Console.ReadLine());
                        kalkulatorMnMacierzy.CancelAsync(deleteiD);
                        break;
                    case 'p':
                        Console.WriteLine("\nPrinting.");
                        foreach (DictionaryEntry de in kalkulatorMnMacierzy.userStateToLifetime)
                        {
                            Console.WriteLine("{0} {1}", de.Key, de.Value);
                        }
                        break;
                    case 'q':
                        break;
                }

            }
        }
    }


    delegate void MatMulCompletedEventHandler(object sender, MatMulCompletedEventArgs e);

    class MatMulCompletedEventArgs : AsyncCompletedEventArgs
    {
        //macierz mat przechowuje wynik operacji mnozenia
        private double[,] mat;
        private int size;

        #region getters
        //commented out RaiseExceptions as they were causing program to stop on thread cancelation
        //which is obviously not how my multithread matrix calculator is supposed to work
        //we just want to cancel a thread, not stop the app
        public double[,] Mat
        {
            get
            {
                //RaiseExceptionIfNecessary();
                return mat;
            }
        }
        public int Size
        {
            get
            {
                //RaiseExceptionIfNecessary();
                return size;
            }
        }
        #endregion

        public MatMulCompletedEventArgs(double[,] mat, int size, Exception error, bool cancelled, object userState)
    : base(error, cancelled, userState)
        {
            this.mat = mat;
            this.size = size;
        }
    }

    class MatMulCalculator
    {
        public event MatMulCompletedEventHandler MatMulCompleted;
        public HybridDictionary userStateToLifetime = new HybridDictionary();
        SendOrPostCallback onCompletedCallback;

        //delegat, ktory wykonuja asynchroniczne zadanie
        delegate void WorkerEventHandler(double[,] mat1, double[,] mat2, int size, AsyncOperation asyncOp);

        //kontruktor
        public MatMulCalculator()
        {
            //initialize delegates
            onCompletedCallback = new SendOrPostCallback(CalculateCompleted);
        }

        public void CalculateCompleted(object state)
        {
            MatMulCompletedEventArgs e = state as MatMulCompletedEventArgs;

            if (MatMulCompleted != null)
            {
                MatMulCompleted(this, e);
            }

            if (e.Cancelled)
            {
                Console.WriteLine("Thread {0} reports with partial result:", e.UserState);
            }
            else
            {
                Console.WriteLine("Thread {0} reports with result:", e.UserState);
            }

            for (int i = 0; i < e.Size; i++)
            {
                for (int j = 0; j < e.Size; j++)
                {
                    //fancy printing techniques
                    if (j == e.Size - 1) Console.WriteLine(e.Mat[i, j]);
                    else Console.Write(e.Mat[i, j] + "\t");
                }
            }
        }

        void Completion(int size, double[,] mat, Exception ex, bool cancelled, AsyncOperation ao)
        {
            if (!cancelled)
            {
                lock (userStateToLifetime.SyncRoot)
                {
                    userStateToLifetime.Remove(ao.UserSuppliedState);
                }
            }
            //pakujemy rezultat operacji - chodzi nam o macierz wynikowa mat
            MatMulCompletedEventArgs e = new MatMulCompletedEventArgs(mat, size, ex, cancelled, ao.UserSuppliedState);
            ao.PostOperationCompleted(onCompletedCallback, e);
        }



        #region WorkerMethods
        bool TaskCancelled(object taskID) { return (userStateToLifetime[taskID] == null); }

        void CalculateWorker(double[,] mat1, double[,] mat2, int size, AsyncOperation asyncOp)
        {
            Exception e = null;
            if (!TaskCancelled(asyncOp.UserSuppliedState))
            {
                try
                {
                    //calculate
                    double[,] wynik = MatMul(mat1, mat2, size, asyncOp);
                    //call for completion
                    this.Completion(size, wynik, e, TaskCancelled(asyncOp.UserSuppliedState), asyncOp);
                }
                catch (Exception ex)
                {
                    e = ex;
                }
            }
        }

        //getVal
        double getVal(double[,] mat, int column, int row){ return mat[column, row]; }

        //mnozenie macierzy
        double[,] MatMul(double[,] mat1, double[,] mat2, int sizeFunc, AsyncOperation asyncOp) {
            int size = sizeFunc;
            bool cancelled = false;
            double[,] tmpMat = new double[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    

                    for (int k = 0; k < size; k++)
                    {
                        //if user cancelled the task - break
                        if (TaskCancelled(asyncOp.UserSuppliedState)) { cancelled = true; }

                        if (cancelled) break;
                        tmpMat[i, j] += getVal(mat1, i, k) * getVal(mat2, k, j);

                        //slow down, you crazy threads
                        Thread.Sleep(Program.sleepTime);
                    }
                    if (cancelled) break;
                }
                if (cancelled) break;
            }

            return tmpMat;
        }
        #endregion


        public virtual void MatMulAsync(double[,] mat1, double[,] mat2, int size, object taskId)
        {
            AsyncOperation asyncOp = AsyncOperationManager.CreateOperation(taskId);
            lock (userStateToLifetime.SyncRoot)
            {
                if (userStateToLifetime.Contains(taskId))
                {
                    throw new ArgumentException(
                        "Task ID parameter must be unique",
                        "taskId");
                }

                userStateToLifetime[taskId] = asyncOp;
            }

            // Start the asynchronous operation.
            WorkerEventHandler workerDelegate = new WorkerEventHandler(CalculateWorker);
            workerDelegate.BeginInvoke(
            mat1, mat2, size,
            asyncOp,
            null,
            null);
        }

        public void CancelAsync(object taskId) {
            AsyncOperation asyncOp = userStateToLifetime[taskId] as AsyncOperation;
            if (asyncOp != null)
            {
                lock (userStateToLifetime.SyncRoot)
                {
                    userStateToLifetime.Remove(taskId);
                }
            }
        }
    }
}










