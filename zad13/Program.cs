using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zad13
{
    class Program
    {
        private bool _Z2 = false;
        public bool Z2
        {
            get { return _Z2; }
            set { _Z2 = value; }
        }
        public async void Zadanie2()
        {
            //ZADANIE 2. ODKOMENTUJ I POPRAW
            await Task.Run(
                () =>
            {
                Z2 = true;
            });
        }

        static void Main(string[] args)
        {
        }
    }
}
