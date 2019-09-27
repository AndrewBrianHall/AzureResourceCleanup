using System.IO;
using System.Text;
using System.Threading.Tasks;
using AzureResourceAutoManagement.Functions;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Network.Fluent;

namespace AzureResourceAutoManagement
{
    public class HtmlHelper
    {
        private const string PortalUrlBase = "https://portal.azure.com/#resource";
        private const string ChangeStateFormFile = "ChangeStateForm.html";

        public string IpAddress { get; protected set; }
        public string DnsName { get; protected set; }
        public string DisplayPowerState { get; protected set; }
        public string ManagementLink { get; protected set; }

        IVirtualMachine _machine { get; set; }

        protected HtmlHelper(IVirtualMachine machine)
        {
            _machine = machine;
        }

        public static HtmlHelper GetVmEntryDiv(IVirtualMachine machine)
        {
            HtmlHelper helper = new HtmlHelper(machine)
            {
                DisplayPowerState = FormatPowerState(machine.PowerState),
                ManagementLink = GetManagementLink(machine)
            };


            IPublicIPAddress ipAddress = machine.GetPrimaryPublicIPAddress();
            helper.IpAddress = ipAddress.IPAddress;
            helper.DnsName = ipAddress.Inner.DnsSettings.Fqdn;

            return helper;
        }

        public string GetHtml(string functionBasePath)
        {
            StringBuilder finalDiv = new StringBuilder("<div>");

            finalDiv.Append($"{this._machine.Name} ({this.DisplayPowerState})");
            if (_machine.PowerState == PowerState.Running)
            {
                finalDiv.Append($" IP Address: {this.IpAddress}");
                finalDiv.Append(GetStopForm(functionBasePath));
            }
            else if(_machine.PowerState == PowerState.Deallocated || _machine.PowerState == PowerState.Stopped)
            {
                finalDiv.Append(GetStartForm(functionBasePath));
            }

            finalDiv.Append($" {this.ManagementLink}</div>");

            return finalDiv.ToString();
        }


        private static string GetFormTemplate(string basePath)
        {
            string file = Path.Combine(basePath, "Html", ChangeStateFormFile);
            string contents;
            using (var reader = new StreamReader(file))
            {
                contents = reader.ReadToEnd();
            }

            return contents;
        }

        string GetStartForm(string basePath)
        {
            string contents = GetFormTemplate(basePath);
            return string.Format(contents, nameof(StartVm), _machine.Name, "Start");
        }

        string GetStopForm(string basePath)
        {
            string contents = GetFormTemplate(basePath);
            return string.Format(contents, nameof(StopVm), _machine.Name, "Stop");
        }


        public static string GetManagementLink(IVirtualMachine machine)
        {
            string portalUrl = CombineUris(PortalUrlBase, machine.Id);
            string aTag = $"<a href=\"{portalUrl}\">Manage</a>";
            return aTag;
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
