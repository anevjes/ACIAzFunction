using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Rest;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Fluent;
using System;
using Microsoft.Azure.Management.ContainerInstance.Fluent.Models;
using Newtonsoft.Json;

namespace ACIFunction
{
    public static class ACIFunction
    {


        [FunctionName("ACIFunction-Orcherstrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();
            outputs.Add(await context.CallActivityAsync<string>("DoWork", context.GetInput<Data>()));

            return outputs;
        }

        [FunctionName("DoWork")]
        public static async Task<string> DoWork([ActivityTrigger] Data inputData)
        {
            var azure = await GetAzureContext(inputData.ContainerGroup.subscriptionId);
            RunTaskBasedContainer(azure, inputData.ContainerGroup.rgName, inputData.ContainerGroup.name, inputData.ContainerGroup.acrServer, inputData.ContainerGroup.acrUserName, inputData.ContainerGroup.acrPassword, inputData.ContainerGroup.acrImageName, null);

            return null;
        }

        [FunctionName("ACI_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var requestBody = await req.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<Data>(requestBody);

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("ACIFunction-Orcherstrator", data);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        private async static Task<IAzure> GetAzureContext(string subscriptionId)
        {

            // Get service credentials through Managed Identity
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            var serviceCreds = new TokenCredentials(await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/"));

            // Define the AzureCredentials object, and point to the public cloud.
            AzureCredentials azureCredentials = new AzureCredentials(
                serviceCreds,
                serviceCreds,
                serviceCreds.TenantId,
                AzureEnvironment.AzureGlobalCloud);

            // Define the RestClient, using public cloud, loglevel and the credentials we created above.
            RestClient client = RestClient
                .Configure()
                .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.BodyAndHeaders)
                .WithCredentials(azureCredentials)
                .Build();

            // Authenticate to Azure using the credentials, and target your subscription.
            IAzure azure = Microsoft.Azure.Management.Fluent.Azure
                .Authenticate(client, serviceCreds.TenantId)
                .WithSubscription(subscriptionId);

            return azure;
        }

        private static void RunTaskBasedContainer(IAzure azure,
                                         string resourceGroupName,
                                         string containerGroupName,
                                         string acrServer,
                                         string acrUserName,
                                         string acrPassword,
                                         string containerImage,
                                         string startCommandLine)
        {
   

            // Configure some environment variables in the container which the
            // wordcount.py or other script can read to modify its behavior.
            Dictionary<string, string> envVars = new Dictionary<string, string>
    {
        { "test1", "5" },
        { "test2", "8" }
    };

            //log.LogInformation($"Creating container group '{containerGroupName}' with start command '{startCommandLine}'");

            // Get the resource group's region
            IResourceGroup resGroup = azure.ResourceGroups.GetByName(resourceGroupName);
            Region azureRegion = resGroup.Region;


            // If a start command wasn't specified, use a default
            if (String.IsNullOrEmpty(startCommandLine))
            {
                // Create the container group
                var containerGroup = azure.ContainerGroups.Define(containerGroupName)
                    .WithRegion(azureRegion)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithLinux()
                    //.WithPublicImageRegistryOnly()
                    .WithPrivateImageRegistry(acrServer, acrUserName, acrPassword)
                    .WithoutVolume()
                    .DefineContainerInstance(containerGroupName + "-1")
                        .WithImage(containerImage)
                        .WithExternalTcpPort(80)
                        .WithCpuCoreCount(1.0)
                        .WithMemorySizeInGB(1)
                        .WithEnvironmentVariables(envVars)
                        .Attach()
                    .WithDnsPrefix(containerGroupName)
                    .WithRestartPolicy(ContainerGroupRestartPolicy.Never)
                    .Create();
            }

            else
            {
                // Create the container group
                var containerGroup = azure.ContainerGroups.Define(containerGroupName)
                    .WithRegion(azureRegion)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithLinux()
                    //.WithPublicImageRegistryOnly()
                    .WithPrivateImageRegistry(acrServer, acrUserName, acrPassword)
                    .WithoutVolume()
                    .DefineContainerInstance(containerGroupName + "-1")
                        .WithImage(containerImage)
                        .WithExternalTcpPort(80)
                        .WithCpuCoreCount(1.0)
                        .WithMemorySizeInGB(1)
                        .WithStartingCommandLine(startCommandLine)
                        .WithEnvironmentVariables(envVars)
                        .Attach()
                    .WithDnsPrefix(containerGroupName)
                    .WithRestartPolicy(ContainerGroupRestartPolicy.Never)
                    .Create();

                azure.ContainerGroups.Start(resourceGroupName, containerGroupName);

            }

            // Print the container's logs
            //log.LogInformation($"Logs for container '{containerGroupName}-1':");
            //log.LogInformation(containerGroup.GetLogContent(containerGroupName + "-1"));
        }
    }
}