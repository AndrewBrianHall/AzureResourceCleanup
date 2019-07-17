using System;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Network.Fluent;

namespace AzureResourceAutoManagement
{
    internal enum ShouldShutDownReasons { NotRunning, Running, OptedOut }

    internal class AzureVmManager
    {
        private const string OptedOutVmsSettingName = "OptedOutVms";

        ILogger _logger;
        IAzure _azure;
        FunctionHelpers _helper;

        protected AzureVmManager(string callingFunction, ILogger logger)
        {
            _logger = logger;
            _helper = new FunctionHelpers(callingFunction);
        }

        public static AzureVmManager CreateVmManagerInstance(string callingFunction, ILogger logger)
        {
            AzureVmManager vmManager = new AzureVmManager(callingFunction, logger);
            vmManager.InitializeAzureConnection();
            return vmManager;
        }

        protected void InitializeAzureConnection()
        {
            AzureCredentials credentials = _helper.GetAzureCredentials();

            _azure = Azure
            .Configure()
            .Authenticate(credentials)
            .WithDefaultSubscription();
        }

        internal async Task<IVirtualMachine> GetVmByNameAsync(string name)
        {
            IPagedCollection<IVirtualMachine> machines = await GetVirtualMachinesAsync();
            IVirtualMachine machine = machines.Where(vm => vm.Name == name).FirstOrDefault();
            return machine;
        }

        internal async Task<string> GetVmStatusAsync(string name)
        {
            IVirtualMachine machine = await GetVmByNameAsync(name);
            PowerState powerState = machine.PowerState;
            
            if(powerState == PowerState.Running)
            {
                IPublicIPAddress ipAddress = machine.GetPrimaryPublicIPAddress();
                return $"{powerState}, IP Address: {ipAddress.IPAddress}";
            }
            return $"Machine status: {powerState}";
        }

        private async Task<IPagedCollection<IVirtualMachine>> GetVirtualMachinesAsync()
        {
            return await _azure.VirtualMachines.ListAsync();
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

            string optoutList = _helper.GetEnvironmentSetting(OptedOutVmsSettingName);
            if (!string.IsNullOrEmpty(optoutList))
            {
                string[] optedoutVms = optedoutVms = optoutList.Split(';');
                shouldShutdown = !optedoutVms.Contains(vm.Name);
                reason = ShouldShutDownReasons.OptedOut;
                _helper.LogMessage($"VM {vm.Name} opted out of auto-shutdown");
            }

            return shouldShutdown;
        }

        internal async Task ShutdownRunningVmsAsync()
        {
            _helper.LogMessage($"Starting at {_helper.LocalNow}");

            var list = await GetVirtualMachinesAsync();

            foreach (var vm in list)
            {
                var state = vm.PowerState;

                bool shouldShutDown = ShouldShutdown(vm, out ShouldShutDownReasons reason);

                if (shouldShutDown)
                {
                    _helper.LogMessage($"Powering off {vm.Name} async");

                    //Don't need to leave the function running while the machine shutsdown as this can take a while so do not await this call
                    vm.PowerOffAsync();
                }
                else
                {
                    _helper.LogMessage($"Not powering off {vm.Name}. Reason: {reason}");
                }
            }

            _helper.LogMessage($"Completing at {_helper.LocalNow}");
        }

        internal async Task<string> StartVirtualMachineAsync(string name)
        {
            IVirtualMachine machine = await GetVmByNameAsync(name);
            if (machine.PowerState == PowerState.Deallocated || machine.PowerState == PowerState.Stopped)
            {
                //This will take a while, let it run async and continue
                machine.StartAsync();
                return $"Machine is starting, original state was {machine.PowerState}";
            }
            else if (machine.PowerState == PowerState.Running)
            {
                IPublicIPAddress ipAddress = machine.GetPrimaryPublicIPAddress();
                return $"Machine is already running, IP address is {ipAddress.IPAddress}";
            }
            else
            {
                return $"No action taken, machine state is: {machine.PowerState}";
            }
        }
    }
}
