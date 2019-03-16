// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MinditshopperBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
            //HttpClient client = new HttpClient();
            //client.DefaultRequestHeaders.Accept.Clear();
            //client.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));
            //client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository");
            //client.DefaultRequestHeaders.Add("x-api-key", "M2tlZWdqdXNuZmFrdw==");
            //var msg = client.GetStringAsync("https://mindshopperrecws.azurewebsites.net/api/models/default/recommend?itemId=" + "11072088").Result;

            //IList<Item> items = JsonConvert.DeserializeObject<IList<Item>>(msg);

        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    // Add Azure Logging
                    logging.AddAzureWebAppDiagnostics();

                    // Logging Options.
                    // There are other logging options available:
                    // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1
                    // logging.AddDebug();
                    // logging.AddConsole();
                })

                // Logging Options.
                // Consider using Application Insights for your logging and metrics needs.
                // https://azure.microsoft.com/en-us/services/application-insights/
                // .UseApplicationInsights()
                .UseStartup<Startup>()
                .Build();

       
    }
}
