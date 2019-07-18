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
            )]TimerInfo myTimer, 
            ILogger log,
            ExecutionContext context)
        {
            FunctionHelpers helper = new FunctionHelpers(nameof(ShutdownVmsOnSchedule), log, context);
            AzureVmManager vmManager = AzureVmManager.CreateVmManagerInstance(helper);

            await vmManager.ShutdownRunningVmsAsync();
        }

    }
}
