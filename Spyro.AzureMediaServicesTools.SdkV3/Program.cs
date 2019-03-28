using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using Spyro.AzureMediaServicesTools.SdkV3.Domain;

namespace Spyro.AzureMediaServicesTools.SdkV3
{
    class Program
    {

        static int Main()
        {
            return CommandLine.Run<Program>(CommandLine.Arguments, defaultCommandName: "ExportAssets");
        }

        public static int ExportAssetsInCsv(string exportPath)
        {
            var config = new ConfigWrapper(new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build());
            var amsClient = CreateMediaServicesClientAsync(config).Result;

            var assets = new List<Asset>();
            var firstPage = amsClient.Assets.List(config.ResourceGroup, config.AccountName);
            var currentPage = firstPage;
            assets.AddRange(currentPage);
            while (firstPage.NextPageLink != null)
            {
                currentPage = amsClient.Assets.ListNext(currentPage.NextPageLink);
            }

            var assetRows = new List<AssetRow>();
            foreach (var asset in assets)
            {
                var streamingLocator = GetStreamingLocatorFromPagingResults(asset.Name, config, amsClient);
                var streamingPolicy = amsClient.StreamingLocators.ListPaths(config.ResourceGroup, config.AccountName,
                    streamingLocator.Name);
                assetRows.Add(new AssetRow(){AssetId = asset.Name, Manifest = streamingPolicy.StreamingPaths[0].Paths[0]});
            }

            using (var writer = new StreamWriter(exportPath))
            using (var csv = new CsvWriter(writer))
            {
                csv.WriteRecords(assetRows);
            }

            return 0;
        }

        private static StreamingLocator GetStreamingLocatorFromPagingResults(string assetName, ConfigWrapper config, IAzureMediaServicesClient mediaServicesClient)
        {
            StreamingLocator streamingLocator = null;
            var firstPage = mediaServicesClient.StreamingLocators.List(config.ResourceGroup,
                config.AccountName);
            var currentPage = firstPage;
            streamingLocator = currentPage.FirstOrDefault(x => x.AssetName == assetName);

            while (currentPage.NextPageLink != null)
            {
                if (currentPage.Any(x => x.AssetName == assetName))
                {
                    streamingLocator = currentPage.FirstOrDefault(x => x.AssetName == assetName);
                    break;
                }

                currentPage = mediaServicesClient.StreamingLocators.ListNext(currentPage.NextPageLink);
            }

            return streamingLocator;
        }

        /// <summary>
        /// Creates the AzureMediaServicesClient object based on the credentials
        /// supplied in local configuration file.
        /// </summary>
        /// <param name="config">The parm is of type ConfigWrapper. This class reads values from local configuration file.</param>
        /// <returns></returns>
        // <CreateMediaServicesClient>
        private static async Task<IAzureMediaServicesClient> CreateMediaServicesClientAsync(ConfigWrapper config)
        {
            var credentials = await GetCredentialsAsync(config);

            return new AzureMediaServicesClient(config.ArmEndpoint, credentials)
            {
                SubscriptionId = config.SubscriptionId,
            };
        }
        // </CreateMediaServicesClient>

        /// <summary>
        /// Create the ServiceClientCredentials object based on the credentials
        /// supplied in local configuration file.
        /// </summary>
        /// <param name="config">The parm is of type ConfigWrapper. This class reads values from local configuration file.</param>
        /// <returns></returns>
        // <GetCredentialsAsync>
        private static async Task<ServiceClientCredentials> GetCredentialsAsync(ConfigWrapper config)
        {
            ClientCredential clientCredential = new ClientCredential(config.AadClientId, config.AadSecret);
            return await ApplicationTokenProvider.LoginSilentAsync(config.AadTenantId, clientCredential, ActiveDirectoryServiceSettings.Azure);
        }
        // </GetCredentialsAsync>
    }
}
