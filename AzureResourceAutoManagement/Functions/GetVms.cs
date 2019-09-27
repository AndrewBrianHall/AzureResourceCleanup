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
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Compute.Fluent;
using System.Collections.Generic;

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
            IPagedCollection<IVirtualMachine> machines = await vmManager.GetVirtualMachinesAsync();

            
            VmHtmlMaker vmHtmlMaker = new VmHtmlMaker(context.FunctionAppDirectory, req);
            string html = vmHtmlMaker.GetHtml(machines);

            var result = new ContentResult
            {
                Content = html,
                ContentType = "text/html"
            };

            return result;
        }
    }

}
