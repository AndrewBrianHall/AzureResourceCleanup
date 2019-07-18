using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AzureResourceAutoManagement.Functions
{
    public static class GetVmStatus
    {
        [FunctionName(nameof(GetVmStatus))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult("Please pass the VM name on the query string as \"name=[virtual-machine-name]\"");
            }

            FunctionHelpers helper = new FunctionHelpers(nameof(GetVmStatus), log, context);
            AzureVmManager vmManager = AzureVmManager.CreateVmManagerInstance(helper);
            var status = await vmManager.GetVmStatusAsync(name);

            return new OkObjectResult($"{status}");
        }
    }
}
