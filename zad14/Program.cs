using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace zad14
{
    class Program
    {
        public static async Task<XmlDocument> siteContent(string address)
        {
            WebClient webClient = new WebClient();
            string xmlContent = await webClient.DownloadStringTaskAsync(new Uri(address));
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlContent);
            return doc;
        }




        static void Main(string[] args)
        {
            Task<XmlDocument> task1 = siteContent("http://www.feedforall.com/sample.xml");

            task1.Wait();
            var wynik = task1.GetAwaiter().GetResult();

            //something to put a breakpoint on
            while (false) ;
        }
    }
}
