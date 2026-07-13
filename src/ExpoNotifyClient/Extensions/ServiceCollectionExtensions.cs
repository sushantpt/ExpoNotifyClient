using System;
using System.Net.Http;
using System.Text.Json;
using ExpoNotifyClient.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpoNotifyClient.Extensions;

/// <summary>
/// Extension methods for registering ExpoNotifyClient with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Expo notification client with a custom HttpClient.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureClient">Action to configure the HttpClient.</param>
    /// <param name="configureOptions">Optional configuration delegate.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExpoNotifyClient(this IServiceCollection services, Action<HttpClient> configureClient, Action<ExpoClientOptions>? configureOptions = null)
    {
        // Add options configuration
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<ExpoClientOptions>(options => { });
        }

        // Register HttpClient with custom configuration
        services.AddHttpClient<IExpoClient, ExpoClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ExpoClientOptions>>().Value;
            
            configureClient(client);
            
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            
            if (options.EnableCompression)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            }
            
            if (!string.IsNullOrEmpty(options.AccessToken))
            {
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.AccessToken);
            }
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip | 
                                            System.Net.DecompressionMethods.Deflate;
            return handler;
        });

        // Register the client as scoped
        services.TryAddScoped<IExpoClient, ExpoClient>();

        return services;
    }

    /// <summary>
    /// Adds the Expo notification client with a custom HttpClient instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="httpClient">The HttpClient instance to use.</param>
    /// <param name="configureOptions">Optional configuration delegate.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExpoNotifyClient(this IServiceCollection services, HttpClient httpClient, Action<ExpoClientOptions>? configureOptions = null)
    {
        // Add options configuration
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<ExpoClientOptions>(options => { });
        }

        // Register the client with the provided HttpClient
        services.AddScoped<IExpoClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ExpoClientOptions>>().Value;
            var logger = serviceProvider.GetService<ILogger<ExpoClient>>();
            
            // Configure the HttpClient
            httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            
            if (options.EnableCompression)
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            }
            
            if (!string.IsNullOrEmpty(options.AccessToken))
            {
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.AccessToken);
            }
            
            return new ExpoClient(httpClient, options, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds the Expo notification client with a fluent builder pattern.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Configuration delegate for options.</param>
    /// <returns>A builder for further configuration.</returns>
    public static ExpoClientBuilder AddExpoNotifyClient(this IServiceCollection services, Action<ExpoClientOptions> configureOptions)
    {
        return new ExpoClientBuilder(services, configureOptions);
    }

    /// <summary>
    /// Adds the Expo notification client with default configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>A builder for further configuration.</returns>
    public static ExpoClientBuilder AddExpoNotifyClient(this IServiceCollection services)
    {
        return new ExpoClientBuilder(services, options => { });
    }
}