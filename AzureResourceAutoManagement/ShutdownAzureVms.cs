using System;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace AzureResourceAutoManagement
{
    internal enum ShouldShutDownReasons {NotRunning, Running, OptedOut }

    internal class ShutdownAzureVms
    {
        private const string TimeZoneSettingName = "WEBSITE_TIME_ZONE";
        private const string OptedOutVmsSettingName = "OptedOutVms";
        private const string ClientIdSettingName = "ClientId";
        private const string ClientSecretSettingName = "ClientSecret";
        private const string TenantIdSettingName = "TenantId";

        ILogger _logger;

        internal ShutdownAzureVms(ILogger logger)
        {
            _logger = logger;
        }

        internal void InternalRun()
        {
            LogMessage($"Starting at {this.LocalNow}");

            AzureCredentials credentials = GetAzureCredentials();

            var azure = Azure
            .Configure()
            .Authenticate(credentials)
            .WithDefaultSubscription();

            var list = azure.VirtualMachines.List();
            foreach (var vm in list)
            {
                var state = vm.PowerState;

                bool shouldShutDown = ShouldShutdown(vm, out ShouldShutDownReasons reason);

                if (shouldShutDown)
                {
                    LogMessage($"Powering off {vm.Name} async");

                    vm.PowerOffAsync();
                }
                else
                {
                    LogMessage($"Not powering off {vm.Name}. Reason: {reason}");
                }
            }

            LogMessage($"Completing at {this.LocalNow}");
        }

        public DateTime LocalNow
        {
            get
            {
                try
                {
                    var timeZoneId = GetEnvironmentSetting(TimeZoneSettingName);
                    DateTime now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, timeZoneId);
                    return now;
                }
                catch (Exception e)
                {
                    LogMessage($"Error getting local time: {e.Message}");
                }

                return DateTime.Now;
            }
        }

        void LogMessage(string message)
        {
            _logger.LogInformation($"{nameof(AutoVmShutdown)}: {message}");
        }

        bool ShouldShutdown(IVirtualMachine vm, out ShouldShutDownReasons reason)
        {
            if (vm.PowerState != PowerState.Running)
            {
                reason = ShouldShutDownReasons.NotRunning;
                return false;
            }

            bool shouldShutdown = true;
            reason = ShouldShutDownReasons.Running;

            string optoutList = GetEnvironmentSetting(OptedOutVmsSettingName);
            if (!string.IsNullOrEmpty(optoutList))
            {
                string[] optedoutVms = optedoutVms = optoutList.Split(';');
                shouldShutdown = !optedoutVms.Contains(vm.Name);
                reason = ShouldShutDownReasons.OptedOut;
                LogMessage($"VM {vm.Name} opted out of auto-shutdown");
            }

            return shouldShutdown;
        }

        public static AzureCredentials GetAzureCredentials()
        {
            string clientid = GetEnvironmentSetting(ClientIdSettingName);
            string clientSecret = GetEnvironmentSetting(ClientSecretSettingName);
            string tenantId = GetEnvironmentSetting(TenantIdSettingName);

            AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientid, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);

            return credentials;
        }

        public static string GetEnvironmentSetting(string settingName)
        {
            return Environment.GetEnvironmentVariable(settingName, EnvironmentVariableTarget.Process);
        }
    }
}
