using System;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using AzureResourceAutoManagement.Functions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;

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
        private IConfigurationRoot _config;

        public FunctionHelpers(string callingFunction, ILogger logger, ExecutionContext context)
        {
            _logger = logger;
            _callingFunction = callingFunction;

            _config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("secret.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
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
            string setting = _config[settingName];

            return setting;
        }

        public void LogMessage(string message)
        {
            _logger.LogInformation($"{nameof(ShutdownVmsOnSchedule)}: {message}. Time: {LocalNow}");
        }

    }
}
