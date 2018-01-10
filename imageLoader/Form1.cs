using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace imageLoader
{
    public partial class Form1 : Form
    {
        /*
        loading bar jest nieresponsywny - niestety, jest to wina Windowsa, poniewaz ustawiajac nowe value progressBar'owi, zmiana jest animowana na ekranie
        istnieje sposob na ominiecie animacji, ale dlaczego go nie uzylem - napisalem w metodzie "ReportProgress"
        */
        class ProfessionalAsyncImageLoader
        {
            #region Variables
            private CancellationTokenSource cts;
            #endregion

            #region Properties
            #endregion

            #region Constructors
            public ProfessionalAsyncImageLoader()
            {
                cts = new CancellationTokenSource();
            }
            #endregion

            #region Methods
            public void Stop()
            {
                cts.Cancel();
            }
            public async Task LoadImageAsync(string FilepathOrFilename, PictureBox pb, IProgress<int> progress )
            {
                await Task.Run(() =>
                {
                    bool leave = false;
                    Bitmap source = new Bitmap(FilepathOrFilename);
                    Bitmap tmp = new Bitmap(source.Width, source.Height);
                    double loadedPixels = 0;
                    for (int i = 0; i < source.Width; i++)
                    {
                        for (int j = 0; j < source.Height; j++)
                        {
                            //check for cancellation
                            if (cts.IsCancellationRequested) leave = true;
                            if (leave) break;

                            //do the work
                            tmp.SetPixel(i, j, source.GetPixel(i, j));
                            loadedPixels++;

                            //report success every X step
                            if (i*j % 200 == 0 && i!=0 && j!=0)
                            {
                                int integer = (((int)loadedPixels *100) / (source.Width * source.Height));
                                progress.Report((int)integer);
                            }


                            //slow down the loading
                            // Thread.Sleep(1);
                        }
                        if (leave) break;
                    }
                    pb.Image = tmp;
                }, cts.Token);
            }


            #endregion
        }



        int i = 0;
        string picname = "pingwin.jpg";
        ProfessionalAsyncImageLoader PAIL;
        public Form1()
        {
            InitializeComponent();
        }


        #region buttonFunctions
        private void button1_Click(object sender, EventArgs e)
        {
            //assert we dont wait for picbox
            pictureBox1.WaitOnLoad = false;

            PAIL = new ProfessionalAsyncImageLoader();

            var progressIndicator = new Progress<int>(ReportProgress);
            var result = PAIL.LoadImageAsync(picname, pictureBox1, progressIndicator);
            while (false) ;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            PAIL.Stop();
            progressBar1.Value = 0;
            
            //hehe chcialoby sie, zeby w tym zadaniu chodzilo o uzycie pictureBox1.LoadAsync();
            //pictureBox1.CancelAsync();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            pictureBox1.Image = null;
            pictureBox1.Refresh();
        }
        #endregion


        private void ReportProgress(int obj)
        {
            /*
            bardziej responsywny loading bar
            niestety, powoduje on jakis dziwny error, ktory co prawda mozna bezpiecznie zignorowac i kliknac "continue", 
            a nawet mozna odznaczyc opcje "break when this exception happens",
            ale nie chcialem umieszczac takiego czegos we wlasciwym rozwiazaniu


                        if (obj == 100 || obj == 0)
                        {
                            progressBar1.Value = obj;
                            return;
                        }
                        else
                        {
                            //
                            progressBar1.Value = obj+1;
                            if (obj > 0)
                            {
                                progressBar1.Value = obj - 1;
                            }
                        }
            */
            progressBar1.Value = obj;
        }
    }
}
