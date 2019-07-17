using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AzureResourceAutoManagement
{
    public static class LocalSecretStore
    {
        const string DefaultSecretsFile = "local.secrets.json";
        private static Dictionary<string, string> _secretStore;

        public static string GetLocalSecret(string settingName)
        {
            if (_secretStore == null)
            {
                InitializeDefault();
            }

            _secretStore.TryGetValue(settingName, out string value);

            return value;
        }

        public static bool InitializeDefault()
        {
            string binDir = Directory.GetCurrentDirectory();
            string secretsFile = Path.Combine(binDir, @"..\..\..", DefaultSecretsFile);
            return LoadSecretsFromFile(secretsFile);
        }

        public static bool LoadSecretsFromFile(string secretsFile = null)
        {
            if (File.Exists(secretsFile))
            {
                using (StreamReader reader = new StreamReader(secretsFile))
                {
                    string contents = reader.ReadToEnd();
                    var secretStore = JsonConvert.DeserializeObject<Dictionary<string, string>>(contents);
                    if (_secretStore == null)
                    {
                        _secretStore = secretStore;
                    }
                    else
                    {
                        foreach (KeyValuePair<string, string> secretPar in secretStore)
                        {
                            //Collisons won't be added
                            _secretStore.TryAdd(secretPar.Key, secretPar.Value);
                        }
                    }
                }
                return true;
            }
            else
            {
                _secretStore = new Dictionary<string, string>();
                return false;
            }
        }
    }
}
