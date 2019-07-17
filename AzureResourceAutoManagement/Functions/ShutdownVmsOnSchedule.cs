using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace AzureResourceAutoManagement.Functions
{
    public class ShutdownVmsOnSchedule
    {
        [FunctionName(nameof(ShutdownVmsOnSchedule))]
        public static async Task Run([TimerTrigger("0 00 1 * * *"
#if DEBUG 
            //,RunOnStartup =true 
#endif
            )]TimerInfo myTimer, ILogger log)
        {
            AzureVmManager vmManager = AzureVmManager.CreateVmManagerInstance(nameof(ShutdownVmsOnSchedule), log);
            await vmManager.ShutdownRunningVmsAsync();
        }

    }
}
