using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureResourceAutoManagement.Functions
{
    public static class StopVm
    {
        [FunctionName("StopVm")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            string[] values = requestBody.Split('=');
            string vmName = null;
            bool success = false;
            string message;
            if (values[0] == "vmname")
            {
                vmName = values[1];
                FunctionHelpers helper = new FunctionHelpers(nameof(GetVms), log, context);
                AzureVmManager vmManager = AzureVmManager.CreateVmManagerInstance(helper);
                success = await vmManager.ShutdownVmAsync(vmName);
                message = $"{vmName} is stopping";
            }
            else
            {
                message = "submit expected form";
            }

            VmHtmlMaker htmlMaker = new VmHtmlMaker(context.FunctionAppDirectory, req);
            string html = htmlMaker.GetStateChangedPage(message);


            return
                new ContentResult
                {
                    Content = html,
                    ContentType = "text/html"
                };
        }
    }
}
