using System;
using System.IO;
using System.Runtime.InteropServices;
using KubeClient;
using KubeClient.Extensions.AuthProviders.Gcp;
using KubeClient.MessageHandlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Contrib.KubeClient.CustomResources
{
    public class KubeApiClientFactory
    {
        private readonly ILogger<KubeApiClientFactory> _logger;
        private readonly string _connectionString;

        public KubeApiClientFactory(ILogger<KubeApiClientFactory> logger, IOptions<KubernetesConfigurationStoreOptions> options)
        {
            _logger = logger;
            _connectionString = options.Value.ConnectionString;
        }

        public IKubeApiClient Build()
        {
            IAuthProviderConfigurationStrategy authProviderStrategy = null;
            KubeClientOptions options;
            if (!string.IsNullOrWhiteSpace(_connectionString))
            {
                options = new KubeClientOptions(_connectionString);
                _logger.LogInformation($"Using remote kubernetes connection ({options.ApiEndPoint}).");
            }
            else if (TryGetKubeConfigPath(out var kubeConfigPath))
            {
                var config = K8sConfig.Load(kubeConfigPath);
                if (IsGcpAuthProviderStrategy(config))
                    authProviderStrategy = new GcpAuthProviderStrategy();
                options = config.ToKubeClientOptions(defaultKubeNamespace: "default");
                _logger.LogInformation($"Using kube config ({options.ApiEndPoint}).");
            }
            else
            {
                options = KubeClientOptions.FromPodServiceAccount();
                _logger.LogInformation($"Using cluster-internal kubernetes connection ({options.ApiEndPoint}).");
            }

            if (authProviderStrategy != null)
                return KubeApiClient.Create(authProviderStrategy, options);
            return KubeApiClient.Create(options);
        }

        private bool IsGcpAuthProviderStrategy(K8sConfig config)
        {
            var targetContext = config.Contexts.Find(context => context.Name == config.CurrentContextName);
            var userIdentity = config.UserIdentities.Find(user => user.Name == targetContext.Config.UserName);
            return userIdentity.Config.AuthProvider.Name == "gcp";
        }

        private static bool TryGetKubeConfigPath(out string path)
        {
            path = Environment.GetEnvironmentVariable("KUBECONFIG");
            if (!string.IsNullOrWhiteSpace(path))
                return true;

            string homeDirectoryVariableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "UserProfile" : "HOME";
            string homeDirectory = Environment.GetEnvironmentVariable(homeDirectoryVariableName);
            if (string.IsNullOrWhiteSpace(homeDirectory))
                return false;

            path = Path.Combine(homeDirectory, ".kube", "config");
            return File.Exists(path);
        }
    }
}
