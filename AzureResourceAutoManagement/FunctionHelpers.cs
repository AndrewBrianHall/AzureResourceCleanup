using System;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using AzureResourceAutoManagement.Functions;

namespace AzureResourceAutoManagement
{
    public class FunctionHelpers
    {
        private const string TimeZoneSettingName = "WEBSITE_TIME_ZONE";
        private const string ClientIdSettingName = "ClientId";
        private const string ClientSecretSettingName = "ClientSecret";
        private const string TenantIdSettingName = "TenantId";

        private ILogger _logger;
        private string _callingFunction;

        public FunctionHelpers(string callingFunction)
        {
            _callingFunction = callingFunction;
        }

        private DateTime LocalNow
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

        public AzureCredentials GetAzureCredentials()
        {
            string clientid = GetEnvironmentSetting(ClientIdSettingName);
            string clientSecret = GetEnvironmentSetting(ClientSecretSettingName);
            string tenantId = GetEnvironmentSetting(TenantIdSettingName);

            AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientid, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);

            return credentials;
        }

        public string GetEnvironmentSetting(string settingName)
        {
            string setting = Environment.GetEnvironmentVariable(settingName, EnvironmentVariableTarget.Process);

#if Debug
            //Assume running locally with a debug build and try to get the secret from the local file if it's not present in the environment
            if(setting == null)
            {
                setting = LocalSecretStore.GetLocalSecret(settingName);
            }
#endif
            return setting;
        }

        public void LogMessage(string message)
        {
            _logger.LogInformation($"{nameof(ShutdownVmsOnSchedule)}: {message}. Time: {LocalNow}");
        }

    }
}
