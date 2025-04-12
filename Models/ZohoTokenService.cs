using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

public class ZohoTokenService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public ZohoTokenService(IConfiguration config, HttpClient httpClient)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var clientId = _config["Zoho:ClientId"];
        var clientSecret = _config["Zoho:ClientSecret"];
        var refreshToken = _config["Zoho:RefreshToken"];

        // Vérification des valeurs nulles avant de créer les KeyValuePairs
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(refreshToken))
        {
            throw new Exception("Error: ClientId, ClientSecret, or RefreshToken is null or empty.");
        }

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("refresh_token", refreshToken ?? string.Empty),
            new KeyValuePair<string, string>("client_id", clientId ?? string.Empty),
            new KeyValuePair<string, string>("client_secret", clientSecret ?? string.Empty),
            new KeyValuePair<string, string>("grant_type", "refresh_token")
        });

        var response = await _httpClient.PostAsync("https://accounts.zoho.sa/oauth/v2/token", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error from Zoho API: {errorResponse}");
        }

        // Lire la réponse JSON
        var json = await response.Content.ReadAsStringAsync();
        Console.WriteLine("Zoho API Response: " + json); // Pour inspecter la réponse

        // Désérialiser la réponse JSON
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(json);

        // Vérifier si la clé "access_token" existe
        if (tokenResponse.TryGetProperty("access_token", out var accessToken))
{
    var token = accessToken.GetString();
    if (!string.IsNullOrEmpty(token))
    {
        return token;
    }
    else
    {
        throw new Exception("Error: access_token is null or empty in Zoho response.");
    }
}
else
{
    throw new Exception("Error: Access token not found. Response: " + json);
}

    }
}




