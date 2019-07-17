using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace AzureResourceAutoManagement
{
    public class AutoVmShutdown
    {

        [FunctionName(nameof(AutoVmShutdown))]
        public static void Run([TimerTrigger("0 00 1 * * *"
#if DEBUG 
            ,RunOnStartup =true 
#endif
            )]TimerInfo myTimer, ILogger log)
        {
            ShutdownAzureVms function = new ShutdownAzureVms(log);
            function.InternalRun();
        }
    }
}
