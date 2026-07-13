using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ExpoNotifyClient.Extensions;

/// <summary>
    /// Fluent builder for configuring the Expo notification client.
    /// </summary>
    public class ExpoClientBuilder
    {
        private readonly IServiceCollection _services;
        private readonly Action<ExpoClientOptions> _configureOptions;
        private Action<HttpClient>? _configureHttpClient;
        private HttpClient? _httpClient;

        internal ExpoClientBuilder(IServiceCollection services, Action<ExpoClientOptions> configureOptions)
        {
            _services = services;
            _configureOptions = configureOptions;
        }

        /// <summary>
        /// Configures the HttpClient used by the Expo client.
        /// </summary>
        /// <param name="configureHttpClient">Action to configure the HttpClient.</param>
        /// <returns>The builder for chaining.</returns>
        public ExpoClientBuilder ConfigureHttpClient(Action<HttpClient> configureHttpClient)
        {
            _configureHttpClient = configureHttpClient;
            return this;
        }

        /// <summary>
        /// Uses a specific HttpClient instance.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance to use.</param>
        /// <returns>The builder for chaining.</returns>
        public ExpoClientBuilder UseHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            return this;
        }

        /// <summary>
        /// Builds and registers the Expo client with the service collection.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection Build()
        {
            if (_httpClient != null)
            {
                _services.AddExpoNotifyClient(_httpClient, _configureOptions);
            }
            else if (_configureHttpClient != null)
            {
                _services.AddExpoNotifyClient(_configureHttpClient, _configureOptions);
            }
            else
            {
                _services.AddExpoNotifyClient(_configureOptions);
            }

            return _services;
        }
    }