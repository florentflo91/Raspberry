using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FloAssistant
{
    class News
    {
        internal static readonly string LogFile = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "News.log");

        public News()
        {
        }               

        public async Task<List<Article>> GetNews()
        {
            string BASE_URL = "https://newsapi.org/v2/";

            //Query the v2/top-headlines endpoint for live top news headlines.
            //Query the v2/everything endpoint for recent articles all over the web.
            string endpoint = "top-headlines";

            // sources "sources=" + string.Join(",", request.Sources));
            // "category=" + request.Category.Value.ToString().ToLowerInvariant());
            //"language=" + request.Language.Value.ToString().ToLowerInvariant());
            //"country=" + request.Country.Value.ToString().ToLowerInvariant());
            //"page=" + request.Page);
            //"pageSize=" + request.PageSize);
            string querystring = "country=fr";

            List<Article> newsList = new List<Article>();
            // HttpClient httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
            HttpClient httpClient = new HttpClient();
             //httpClient.DefaultRequestHeaders.Add("user-agent", "News-API-csharp/0.1");
             // httpClient.DefaultRequestHeaders.Add("x-api-key", "fc028a132ff54587b240e2847bfef21a");
             HttpResponseMessage httpResponse = new HttpResponseMessage();
            Log(BASE_URL + endpoint + "?" + querystring);
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, BASE_URL + endpoint + "?" + querystring + "&apiKey=fc028a132ff54587b240e2847bfef21a");
            
           // var uri = new Uri("https://newsapi.org/v2/top-headlines?country=fr&apiKey=fc028a132ff54587b240e2847bfef21a");
            Log("1");
            try
            {
                httpResponse = await httpClient.SendAsync(httpRequest);
                httpResponse.EnsureSuccessStatusCode();
                string httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                Log(httpResponseBody);
                //On désérialise les données en JSON
                NewsAPI newsAPI = Newtonsoft.Json.JsonConvert.DeserializeObject<NewsAPI>(httpResponseBody);
                Log("OK");
                newsList = newsAPI.articles;
                Log("OK2");
            }
            catch (Exception ex)
            {
                newsList = null;
                Log("Erreur : GetNews " + ex.ToString());
                throw;
            }
            return newsList;
        }

        // JSON : Login //
        public class NewsAPI
        {
            public string status;
            public List<Article> articles;
        }

        public class Source
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        public class Article
        {
            public Source source { get; set; }
            public String author { get; set; }
            public String title { get; set; }
            public String description { get; set; }
            public String url { get; set; }
            public String urlToImage { get; set; }
            public String publishedAt { get; set; }
        }

        public class ArticlesResult
        {
           // public Statuses Status { get; set; }
            public Error Error { get; set; }
            public int TotalResults { get; set; }
            public List<Article> Articles { get; set; }
        }

        public class Error
        {
           // public ErrorCodes Code { get; set; }
            public string Message { get; set; }
        }
        // JSON : Login //

        public static void Log(string logMessage, FileStream logFile1 = null)
        {
            // Append text to an existing file named "WriteLines.txt".
            Debug.WriteLine(DateTime.Now.ToString() + " " + logMessage);
            using (StreamWriter outputFile = File.AppendText(LogFile))
            {
                outputFile.WriteLine(DateTime.Now.ToString() + " " + logMessage);
                outputFile.Dispose();
            }
        }
    
    }
}