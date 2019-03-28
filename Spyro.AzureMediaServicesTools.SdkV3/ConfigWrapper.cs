using Microsoft.Extensions.Configuration;
using System;

namespace Spyro.AzureMediaServicesTools.SdkV3
{
    public class ConfigWrapper
    {
        private readonly IConfiguration _config;

        public ConfigWrapper(IConfiguration config)
        {
            _config = config;
        }

        public string SubscriptionId => _config["SubscriptionId"];

        public string ResourceGroup => _config["ResourceGroup"];

        public string AccountName => _config["AccountName"];

        public string AadTenantId => _config["AadTenantId"];

        public string AadClientId => _config["AadClientId"];

        public string AadSecret => _config["AadSecret"];

        public Uri ArmAadAudience => new Uri(_config["ArmAadAudience"]);

        public Uri AadEndpoint => new Uri(_config["AadEndpoint"]);

        public Uri ArmEndpoint => new Uri(_config["ArmEndpoint"]);

        public string Region => _config["Region"];
    }
}
