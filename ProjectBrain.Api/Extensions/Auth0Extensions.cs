using Auth0.ManagementApi;
using Microsoft.Extensions.Caching.Memory;

public static class Auth0Extensions
{
    public static WebApplicationBuilder AddAuth0ManagementApi(this WebApplicationBuilder builder)
    {
        // 1. Add Memory Cache to store the Management API token
        builder.Services.AddMemoryCache();

        // // 2. Register a custom HttpClient for the Management API
        // // This factory will automatically get a new token when the old one expires.
        // builder.Services.AddHttpClient<IManagementApiClient, ManagementApiClient>(client =>
        // {
        //     var config = builder.Configuration.GetSection("Auth0");
        //     var domain = config["Domain"];

        //     client.BaseAddress = new Uri($"https://{domain}/api/v2/");
        // });

        // // 3. Register the ManagementApiClient as a Singleton
        // // We manage its token lifecycle using the IHttpClientFactory and IMemoryCache
        // builder.Services.AddSingleton<IManagementApiClient, ManagementApiClient>(provider =>
        // {
        //     var config = provider.GetRequiredService<IConfiguration>().GetSection("Auth0");
        //     var domain = config["Domain"];
        //     var clientId = config["ManagementApiClientId"];
        //     var clientSecret = config["ManagementApiClientSecret"];

        //     var cache = provider.GetRequiredService<IMemoryCache>();

        //     // This client gets a token for the Management API
        //     var tokenClient = new HttpClient();

        //     var managementApiClient = new ManagementApiClient(async () =>
        //     {
        //         // Check if we have a valid, non-expired token in the cache
        //         if (cache.TryGetValue("Auth0ManagementApiToken", out string token))
        //         {
        //             return token;
        //         }

        //         // If not, fetch a new one
        //         var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"https://{domain}/oauth/token")
        //         {
        //             Content = new FormUrlEncodedContent(new Dictionary<string, string>
        //             {
        //         {"grant_type", "client_credentials"},
        //         {"client_id", clientId},
        //         {"client_secret", clientSecret},
        //         {"audience", $"https://{domain}/api/v2/"}
        //             })
        //         };

        //         var tokenResponse = await tokenClient.SendAsync(tokenRequest);
        //         tokenResponse.EnsureSuccessStatusCode();

        //         var tokenJson = System.Text.Json.JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        //         token = tokenJson.RootElement.GetProperty("access_token").GetString();
        //         var expiresIn = tokenJson.RootElement.GetProperty("expires_in").GetInt32();

        //         // Cache the new token, setting its expiration to 5 minutes before it *actually* expires
        //         cache.Set(
        //             "Auth0ManagementApiToken",
        //             token,
        //             TimeSpan.FromSeconds(expiresIn - 300)
        //         );

        //         return token;

        //     }, new Uri($"https://{domain}/api/v2/"));

        //     return managementApiClient;
        // });

        return builder;
    }
}