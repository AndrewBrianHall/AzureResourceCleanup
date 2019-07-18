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
using Microsoft.Azure.Management.Compute.Fluent;

namespace AzureResourceAutoManagement.Functions
{
    public static class ManageVms
    {
        [FunctionName("ManageVms")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            FunctionHelpers helper = new FunctionHelpers(nameof(GetVmStatus), log, context);
            AzureVmManager vmManager = AzureVmManager.CreateVmManagerInstance(helper);
            var machines = await vmManager.GetVirtualMachinesAsync();

            StringBuilder sb = new StringBuilder();
            HtmlHelper htmlHelper = new HtmlHelper();
            foreach (var machine in machines)
            {
                sb.Append(htmlHelper.GetVmEntryDiv(machine));
            }

            var result = new ContentResult
            {
                Content = sb.ToString(),
                ContentType = "text/html"
            };

            return result;
        }

    }

    public class HtmlHelper
    {
        private const string PortalUrlBase = "https://portal.azure.com/#resource";

        public HtmlHelper()
        {
        }

        public string GetVmEntryDiv(IVirtualMachine machine)
        {
            string portalUrl = CombineUris(PortalUrlBase, machine.Id);
            string powerState = FormatPowerState(machine.PowerState);
            string result = $"<div>{machine.Name} ({powerState}) <a href=\"{portalUrl}\">Manage</a></div>";
            return result;
        }

        private static string FormatPowerState(PowerState powerState)
        {
            return powerState.ToString().Replace("PowerState/", string.Empty);
        }

        public static string CombineUris(params string[] values)
        {
            StringBuilder finalUri = new StringBuilder(values[0]);

            for (int i = 1; i < values.Length; i++)
            {
                string current = values[i];
                bool fullUriEndsInSeparator = false;
                bool currentStartsWithSeparator = false;

                if (current.Length == 0)
                {
                    continue;
                }

                if (finalUri[finalUri.Length - 1] == '/')
                {
                    fullUriEndsInSeparator = true;
                }
                if (current[current.Length - 1] == '/')
                {
                    currentStartsWithSeparator = true;
                }

                if (!fullUriEndsInSeparator && !currentStartsWithSeparator)
                {
                    finalUri.Append('/');
                }

                finalUri.Append(current);
            }

            return finalUri.ToString();
        }
    }

}
