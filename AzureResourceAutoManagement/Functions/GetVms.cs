using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace AzureResourceAutoManagement.Functions
{
    public static class GetVms
    {
        [FunctionName(nameof(GetVms))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            FunctionHelpers helper = new FunctionHelpers(nameof(GetVms), log, context);
            AzureVmManager vmManager = AzureVmManager.CreateVmManagerInstance(helper);
            var machines = await vmManager.GetVirtualMachinesAsync();

            StringBuilder sb = new StringBuilder();
            
            foreach (var machine in machines)
            {
                HtmlHelper vmHtml = HtmlHelper.GetVmEntryDiv(machine);
                sb.Append(vmHtml.GetHtml(context.FunctionAppDirectory));
            }

            var result = new ContentResult
            {
                Content = sb.ToString(),
                ContentType = "text/html"
            };

            return result;
        }

    }

}
