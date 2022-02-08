using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Services;

namespace Web_Service
{

    public class WebService : System.Web.Services.WebService
    {

        [WebMethod]
        public List<News_Links> getNews()
        {
            List<News_Links> news_links = null;

            using (WebClient webClient = new WebClient())
            {
                try
                {
                    using (StreamReader reader = new StreamReader(Server.MapPath("news.json"))){
                        string json = reader.ReadToEnd();

                        var x = JsonConvert.DeserializeObject<String>(json);
                        news_links = JsonConvert.DeserializeObject<List<News_Links>>(x);
                    }

                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }

            return news_links;
        }

        [WebMethod]
        public List<string> getInfos(string title)
        {
            Dictionary<string, List<string>> infos;
            List<string> infos_data = new List<string>();

            using (WebClient webClient = new WebClient())
            {

                using (StreamReader reader = new StreamReader(Server.MapPath("infos.json")))
                {
                    string json = reader.ReadToEnd();

                    var x = JsonConvert.DeserializeObject<string>(json);

                    infos = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(x);

                }

            }
            foreach (KeyValuePair<string, List<string>> entry in infos)
            {
                if (entry.Key == title)
                    infos_data = entry.Value;
            }


            return infos_data.Count >0 ? infos_data : null;
        }
    }
}
