using System;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace AzureResourceAutoManagement
{
    //Shuts all running virtual machines down everyday at 1 AM unless they have been opted out
    public static class AutoVmShutdown
    {
        [FunctionName(nameof(AutoVmShutdown))]
        public static void Run([TimerTrigger("0 00 1 * * *"
#if DEBUG 
            //For debugging purposes, runs the function on first startup
            ,RunOnStartup =true 
#endif
            )]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Starting C# Timer trigger function at: {DateTime.Now}");

            AzureCredentials credentials = GetAzureCredentials();

            var azure = Azure
            .Configure()
            .Authenticate(credentials)
            .WithDefaultSubscription();

            var list = azure.VirtualMachines.List();
            foreach (var vm in list)
            {
                var state = vm.PowerState;

                if (ShouldShutdown(vm, log))
                {
                    log.LogInformation($"Powering off VM async");

                    vm.PowerOffAsync();
                }
            }

            log.LogInformation($"Completing C# Timer trigger function at: {DateTime.Now}");
        }

        private static AzureCredentials GetAzureCredentials()
        {
            string clientid = Environment.GetEnvironmentVariable("ClientId", EnvironmentVariableTarget.Process);
            string clientSecret = Environment.GetEnvironmentVariable("ClientSecret", EnvironmentVariableTarget.Process);
            string tenantId = Environment.GetEnvironmentVariable("TenantId", EnvironmentVariableTarget.Process);
            
            AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientid, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);

            return credentials;
        }

        private static bool ShouldShutdown(IVirtualMachine vm, ILogger log)
        {
            if(vm.PowerState != PowerState.Running)
            {
                return false;
            }

            bool shouldShutdown = true;

            string optoutList = Environment.GetEnvironmentVariable("OptedOutVms", EnvironmentVariableTarget.Process);
            if (!string.IsNullOrEmpty(optoutList))
            {
                string[] optedoutVms = optedoutVms = optoutList.Split(';');
                shouldShutdown = !optedoutVms.Contains(vm.Name);
                log.LogInformation($"VM {vm.Name} opted out of auto-shutdown");
            }

            return shouldShutdown;
        }
    }
}
