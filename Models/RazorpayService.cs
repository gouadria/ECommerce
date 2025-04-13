using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

namespace ECommerce.Models
{
    public class RazorpayService
    {
        private readonly string _keyId;
        private readonly string _keySecret;
        private readonly HttpClient _httpClient;

        public RazorpayService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
           _keyId = configuration["Razorpay:KeyId"];
_keySecret = configuration["Razorpay:KeySecret"];

if (string.IsNullOrWhiteSpace(_keyId) || string.IsNullOrWhiteSpace(_keySecret))
{
    throw new ArgumentNullException("Les clés API Razorpay ne sont pas configurées correctement.");
}

            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        }

        public async Task<string> CreateRazorpayOrderAsync(decimal amount, string email, string phoneNumber)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Le montant de la commande doit être supérieur à zéro.");
            }

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentException("L'email et le numéro de téléphone sont obligatoires.");
            }

            var orderData = new
            {
                amount = (int)(amount * 100), // Conversion en paise (INR)
                currency = "INR",
                payment_capture = 1,
                notes = new
                {
                    email,
                    phone = phoneNumber
                }
            };

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var content = new StringContent(JsonSerializer.Serialize(orderData, jsonOptions), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("https://api.razorpay.com/v1/orders", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Erreur Razorpay ({response.StatusCode}): {responseString}");
                }

                return responseString; // Contient l'order_id
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la création de l'ordre Razorpay : {ex.Message}");
                throw;
            }
        }
    }
}


