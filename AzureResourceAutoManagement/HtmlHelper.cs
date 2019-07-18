using System.Text;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Network.Fluent;

namespace AzureResourceAutoManagement
{
    public class HtmlHelper
    {
        private const string PortalUrlBase = "https://portal.azure.com/#resource";

        public static string GetVmEntryDiv(IVirtualMachine machine)
        {
            StringBuilder finalDiv = new StringBuilder("<div>");
            string powerState = FormatPowerState(machine.PowerState);
            string managementLink = GetManagementLink(machine);

            finalDiv.Append($"{machine.Name} ({powerState})");
            if(machine.PowerState == PowerState.Running)
            {
                IPublicIPAddress ipAddress = machine.GetPrimaryPublicIPAddress();
                finalDiv.Append($" IP Address: {ipAddress.IPAddress}");
            }
            finalDiv.Append($" {managementLink}</div>");

            return finalDiv.ToString();
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
