using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zad12
{
    public class TResultDataStructure
    {
        /*
        autor blednego zapisu chcial zwrocic dwie wartosci int w wyniku operacji asynchronicznej - jedna zwracana bezposrednio przez funkcje
        OperationTask, druga przez modyfikator out.
        Dlatego nalezy zaimplementowac strukture danych, ktora posiada min. dwa inty.
        */
        #region Properties
        int a;
        public int A
        {
            get { return a; }
            set { a = value; }
        }
        int b;
        public int B
        {
            get { return b; }
            set { b = value; }
        }
        #endregion
        //contructor
        public TResultDataStructure(int a, int b)
        {
            this.a = a;
            this.b = b;
        }
    }

    class Program
    {
        /*
                public Task<TResultDataStructure> OperationTask(byte[] buffer)
                {
                }
        */

        static void Main(string[] args)
        {

        }
    }
}
