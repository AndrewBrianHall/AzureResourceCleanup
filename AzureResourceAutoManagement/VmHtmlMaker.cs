using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AzureResourceAutoManagement.Functions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace AzureResourceAutoManagement
{
    public class VmHtmlMaker
    {
        private const string PortalUrlBase = "https://portal.azure.com/#resource";
        private const string ChangeStateFormFile = "ChangeStateForm.html";

  
        protected string _vmListHtmlBase;
        protected readonly string _functionAppDirectory;
        protected readonly string _functionAccessCode;

        public VmHtmlMaker(string functionAppDirectory, HttpRequest req)
        {
            _functionAppDirectory = functionAppDirectory;
            _functionAccessCode = GetFunctionAccessCode(req);
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

        public static string FormatPowerState(PowerState powerState)
        {
            return powerState.ToString().Replace("PowerState/", string.Empty);
        }

        protected static string GetFunctionAccessCode(HttpRequest req)
        {
            IDictionary<string, string> parameters = req.GetQueryParameterDictionary();
            bool hasCode = parameters.TryGetValue("code", out string functionAccessCode);
            return functionAccessCode;
        }

        public string GetHtml(IPagedCollection<IVirtualMachine> machines)
        {
            if(_vmListHtmlBase == null)
            {
                _vmListHtmlBase = GetWebContentFile("VmList.html");
            }

            StringBuilder finalDiv = new StringBuilder();

            foreach(IVirtualMachine machine in machines)
            {
                string vmForm = GetVmControlContent(machine);
                finalDiv.Append(vmForm);
            }

            string finalPage = _vmListHtmlBase.Replace("{{main-content}}", finalDiv.ToString());
            return finalPage;
        }

        public string GetStateChangedPage(string message)
        {
            const string ReturnLocation = nameof(GetVms);
            string template = GetWebContentFile("vmstate-changed.html");
            string returnUrl = !string.IsNullOrEmpty(_functionAccessCode) ? $"{ReturnLocation}?code={_functionAccessCode}" : $"{ReturnLocation}";
            string html = string.Format(template, message, returnUrl);

            return html;
        }


        string GetVmControlContent(IVirtualMachine machine)
        {
            string contents = GetWebContentFile(ChangeStateFormFile);
            string powerState = FormatPowerState(machine.PowerState);
            IPublicIPAddress ipAddress = machine.GetPrimaryPublicIPAddress();
            string action;
            string buttonText;
            string formVisibility = "visible";
            string qualifiedDnsName = string.Empty;
            string displayIp = string.Empty;
            string dnsCopyDisplayClass = "hidden";
            string ipCopyDisplayClass = "hidden";
            if(machine.PowerState == PowerState.Running)
            {
                action = nameof(StopVm);
                buttonText = "Stop";
                qualifiedDnsName = ipAddress.Inner.DnsSettings != null ? ipAddress.Inner.DnsSettings.Fqdn : string.Empty;
                dnsCopyDisplayClass = !string.IsNullOrEmpty(qualifiedDnsName) ? "visible" : dnsCopyDisplayClass;
                displayIp = ipAddress.IPAddress;
                ipCopyDisplayClass = !string.IsNullOrEmpty(displayIp) ? "visible" : ipCopyDisplayClass;
            }
            else if(machine.PowerState == PowerState.Stopped || machine.PowerState == PowerState.Deallocated)
            {
                action = nameof(StartVm);
                buttonText = "Start";
            }
            else
            {
                action = string.Empty;
                buttonText = string.Empty;
                formVisibility = "collapse";
            }
            if (!string.IsNullOrEmpty(_functionAccessCode))
            {
                action = $"{action}?code={_functionAccessCode}";
            }
            string finalForm = string.Format(contents, machine.Name, powerState, qualifiedDnsName, displayIp, action, formVisibility, buttonText, dnsCopyDisplayClass, ipCopyDisplayClass);
            return finalForm;
        }


        public static string GetManagementLink(IVirtualMachine machine)
        {
            string portalUrl = CombineUris(PortalUrlBase, machine.Id);
            string aTag = $"<a href=\"{portalUrl}\">Manage</a>";
            return aTag;
        }

        private string GetWebContentFile(string fileName)
        {
            string directory = !Directory.Exists("d:\\") && Debugger.IsAttached ? Path.Combine(_functionAppDirectory, @"..\..\..") : _functionAppDirectory;
            string file = Path.Combine(directory, "Html", fileName);
            string contents;
            using (var reader = new StreamReader(file))
            {
                contents = reader.ReadToEnd();
            }

            return contents;
        }
    }

}
