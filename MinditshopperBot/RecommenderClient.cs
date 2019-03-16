using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MinditshopperBot
{
    public class RecommenderClient
    {
        private static readonly HttpClient client = new HttpClient();

        public static string lastItemProcessed;

        private static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        

        public static async Task ProcessRepositories()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            var stringTask = client.GetStringAsync("https://api.github.com/orgs/dotnet/repos");

            var msg = await stringTask;
            Console.Write(msg);
        }

        public static IList<Item> ProcessRecommendedItem(String itemId)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Clear();

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository");
            client.DefaultRequestHeaders.Add("x-api-key", "M2tlZWdqdXNuZmFrdw==");
            try
            {
                var msg = client.GetStringAsync("https://mindshopperrecws.azurewebsites.net/api/models/default/recommend?itemId=" + itemId.Trim()).Result;
                IList<Item> items = JsonConvert.DeserializeObject<IList<Item>>(msg);

                return items;
            } catch(Exception ex)
            {
                logger.Error(ex.Message + "\n" + ex?.InnerException?.Message + "\n" + ex.StackTrace);
                return null;
            }
            
        }

        public static IList<Item> ProcessTopItems(String category)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Clear();

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository");
            client.DefaultRequestHeaders.Add("x-api-key", "M2tlZWdqdXNuZmFrdw==");

            var msg = client.GetStringAsync("https://mindshopperrecws.azurewebsites.net/api/topsellers?categoryCode=" + category).Result;

            IList<Item> items = JsonConvert.DeserializeObject<IList<Item>>(msg);

            return items;
        }
    }
}
