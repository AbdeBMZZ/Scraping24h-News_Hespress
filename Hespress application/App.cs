using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using Newtonsoft.Json;
using Web_Service;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Net;
using System.Net.NetworkInformation;

namespace Hespress_application
{
    public partial class App : Form
    {
        int index = 0;

        //web service
        ServiceReference1.WebServiceSoapClient client = new ServiceReference1.WebServiceSoapClient();

        //tableau des actualités
        ServiceReference1.News_Links[] newzz; 

        readonly SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine();
        readonly SpeechSynthesizer synthesizer = new SpeechSynthesizer();
        
        bool news_playing = false;

        readonly ChromiumWebBrowser chromeBrowser;
        const String URL = "https://en.hespress.com/";
        public App()
        {
            InitializeComponent();
            CefSettings settings = new CefSettings();
            chromeBrowser = new ChromiumWebBrowser(URL);
            this.container.Controls.Add(chromeBrowser);
            chromeBrowser.Dock = DockStyle.Fill;
            chromeBrowser.FrameLoadEnd += WebBrowserFrameLoadEnded;
        }

        private void WebBrowserFrameLoadEnded(object sender, FrameLoadEndEventArgs e)
        {
            if(e.Frame.IsMain)
                solution();

        }
        private void App_Load(object sender, EventArgs e)
        {
            //les commandes vocales
            Choices commandes = new Choices();
            commandes.Add(new string[] 
            { 
                "next",
                "stop",
                "start reading from the beginning",
                "read all the news",
                "show details",
                "go back and continue reading",
                "exit reading mode"

            });

            GrammarBuilder grammarBuilder = new GrammarBuilder();
            grammarBuilder.Append(commandes);
            Grammar grammar = new Grammar(grammarBuilder);

            recognizer.LoadGrammarAsync(grammar);
            recognizer.SetInputToDefaultAudioDevice();

            recognizer.RecognizeAsync(RecognizeMode.Multiple);
            recognizer.SpeechRecognized += RegEngine_SpeechRegocnized;

            synthesizer.SpeakCompleted += Syn_SpeekCompleted;



        }

        private void Syn_SpeekCompleted(object sender, SpeakCompletedEventArgs e)
        {
            if (news_playing && index < newzz.Length)
                index++;
        }
        private void RegEngine_SpeechRegocnized(object sender, SpeechRecognizedEventArgs e)
        {

            switch (e.Result.Text)
            {
                case "start reading from the beginning":
                    index = 0;
                    synthesizer.SpeakAsync("starting from the beginning sir!");
                    PromptBuilder builer = new PromptBuilder();
                    builer.StartParagraph();
                    builer.AppendText(newzz[index].Actualite);
                    builer.EndParagraph();

                    builer.AppendBreak();

                    if (CheckForInternetConnection())
                    {
                        builer.StartSentence();
                        builer.AppendText("wanna see details?");
                        builer.EndSentence();
                    }

                    synthesizer.SpeakAsync(builer);
                    break;

                case "next":
                    if(index < newzz.Length)
                    {
                        builer = new PromptBuilder();
                        builer.StartParagraph();
                        builer.AppendText(newzz[++index].Actualite);
                        builer.EndParagraph();

                        builer.AppendBreak();
                        synthesizer.SpeakAsync(builer);
                    }
                    else
                        synthesizer.SpeakAsync("End of news");

                    break;

                case "show details":

                    if (CheckForInternetConnection())
                    {
                        synthesizer.SpeakAsync("the details of :" + newzz[index].Actualite);

                        chromeBrowser.Load(newzz[index].Lien);
                    }
                    else
                        synthesizer.SpeakAsync("Unfortunately you can't use this voice command aa aabdellah");

                    break;

                case "go back and continue reading":
                    if(chromeBrowser.Address != URL)
                    {
                        chromeBrowser.Load(URL);
                        builer = new PromptBuilder();
                        builer.StartParagraph();
                        builer.AppendText(newzz[++index].Actualite);
                        builer.EndParagraph();

                        builer.AppendBreak();
                        synthesizer.SpeakAsync(builer);
                    }
                    news_playing = false;

                    break;

                case "read all the news":
                    news_playing = true;
                    foreach(ServiceReference1.News_Links str in newzz)
                    {
                        synthesizer.SpeakAsync(str.Actualite);
                    }

                    break;



                case "stop":
                    news_playing = false;
                    synthesizer.SpeakAsyncCancelAll();
                    synthesizer.SpeakAsync("as you want!");

                    break;


                case "exit reading mode":
                    synthesizer.SpeakAsync("read mode disabled");
                    recognizer.RecognizeAsyncStop();
                    break;
            }
        }

        private void App_Resize(object sender, EventArgs e)
        {
            this.container.Size = this.ClientSize - new System.Drawing.Size(container.Location);
        }



        private void solution() {
            chromeBrowser.GetSourceAsync().ContinueWith(taskHtml =>
            {
                var html = taskHtml.Result;

                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                if (chromeBrowser.Address == URL)
                {
                    string xpath = "/html/body/main/section[1]/div/div/div[2]/div/ul/div[1]/div/li/a";
                    var links = doc.DocumentNode.SelectNodes(xpath);

                    List<News_Links> news_list = new List<News_Links>();

                    if(links == null)
                    {
                        synthesizer.SpeakAsync("I Have noticed that you're offline, i will search if i have some news stored");
                        if(newzz.Length > 0)
                            synthesizer.SpeakAsync("Fortunately, there is some news!, You can use all voice commands except the show details command.");
                        else
                            synthesizer.SpeakAsync("Unfortunately, there is no news stored!");

                    }

                    else
                    {

                        foreach (var node in links)
                        {
                            News_Links actualite = new News_Links()
                            {
                                Actualite = node.Attributes["title"].Value,
                                Lien = node.Attributes["href"].Value
                            };

                            news_list.Add(actualite);
                        }
                        string json = JsonConvert.SerializeObject(news_list);
                        
                        string path = @"../../../Web_Service/news.json";

                         using (StreamWriter file = File.CreateText(path))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Serialize(file, json);
                        }

                    }

                }
                else
                {
                    var title = doc.DocumentNode.SelectSingleNode("/html/body/div[2]/main/section/div/div[1]/section/article/header/h1").InnerText;
                    var infos = doc.DocumentNode.SelectNodes("/html/body/div[2]/main/section/div/div[1]/section/article/div[2]/p");
                    List<string> news_Infos = new List<string>();

                    foreach (var node in infos)
                        news_Infos.Add(node.InnerText);


                    Dictionary<string, List<string>> title_data = new Dictionary<string, List<string>>
                    {
                        { title, news_Infos }
                    };

                    string json = JsonConvert.SerializeObject(title_data);

                    string path = @"../../../Web_Service/infos.json";

                    using (StreamWriter file = File.CreateText(path))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Serialize(file, json);
                    }

                    if (client.getInfos(newzz[index].Actualite) != null)
                    {
                        //getting more details using the web service.

                        foreach (string str in client.getInfos(newzz[index].Actualite))
                        {
                            synthesizer.SpeakAsync(str);

                        }

                        synthesizer.SpeakAsync("end of details!");

                    }
                }



            });

            newzz = client.getNews();

        }

        public static bool CheckForInternetConnection()
        {
            try
            {
                Ping myPing = new Ping();
                String host = "google.com";
                byte[] buffer = new byte[32];
                int timeout = 1000;
                PingOptions pingOptions = new PingOptions();
                PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
                return (reply.Status == IPStatus.Success);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

}
