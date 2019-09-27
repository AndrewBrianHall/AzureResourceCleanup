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
    public static class StartVm
    {
        [FunctionName("StartVm")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            string[] values = requestBody.Split('=');
            string vmName = null;
            string startMessage = "";
            if (values[0] == "vmname")
            {
                vmName = values[1];
                FunctionHelpers helper = new FunctionHelpers(nameof(GetVms), log, context);
                AzureVmManager vmManager = AzureVmManager.CreateVmManagerInstance(helper);
                startMessage = await vmManager.StartVirtualMachineAsync(vmName);
            }

            return (ActionResult)new OkObjectResult($"{startMessage}");
        }
    }
}
