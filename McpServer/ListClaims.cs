using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace McpServer
{
    public static class ListClaims
    {
        [FunctionName("ListClaims")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            ILogger log)
        {
            var client = new HttpClient();
            var response = await client.GetAsync("https://<your-site>.azurestaticapps.net/data-api/claims");
            var content = await response.Content.ReadAsStringAsync();
            return new JsonResult(new McpContent(content));
        }
    }
}
