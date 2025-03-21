using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class ZohoEmailService
{
    private readonly ZohoTokenService _tokenService;
    private readonly HttpClient _httpClient;

    public ZohoEmailService(ZohoTokenService tokenService, HttpClient httpClient)
    {
        _tokenService = tokenService;
        _httpClient = httpClient;
    }

    public async Task SendConfirmationEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var accessToken = await _tokenService.GetAccessTokenAsync();

            var emailPayload = new
            {
                fromAddress = "gouadriaanis@zohomail.sa",
                toAddress = toEmail,  // ✅ Correction ici (suppression des guillemets)
                subject = subject,
                content = body,
                askReceipt = "yes"
            };

            var jsonPayload = JsonSerializer.Serialize(emailPayload);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://mail.zoho.sa/api/v1/accounts/70679000000002002/messages")
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            // ✅ Ajout de l'en-tête Accept
            request.Headers.Add("Authorization", $"Zoho-oauthtoken {accessToken}");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Erreur : {response.StatusCode}\nDétails : {errorContent}");
            }
            else
            {
                Console.WriteLine("✅ E-mail envoyé avec succès !");
            }

            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"🚨 Erreur HTTP : {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Erreur inattendue : {ex.Message}");
        }
    }
}

