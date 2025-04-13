using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Net.Http.Headers;

public class PayPalService
{
    private readonly string clientId = "AZiyBCUZdE9FH052NsjzV6WR7Q8hferrf6jefF8aAsw7wN_HWFaiNI3cFKW7PKYl2unBnrL-nm5vAXh6";
    private readonly string clientSecret = "EH-sF2WEEDHOneYXchOH74jAXUZb0vVjyGR34bqgjJXRb-bX725c2IYOFBExs1wI_mz3AS9Fljjya-oF";

    public async Task<string> CreatePayPalOrderAsync(decimal amount, string email, string phone)
    {
        string accessToken = await GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return "Erreur: impossible d'obtenir le token d'accès.";
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        string amountString = amount.ToString("F2").Replace(',', '.');

        var data = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    amount = new
                    {
                        currency_code = "USD",
                        value = amountString
                    },
                    description = "Paiement sans compte PayPal"
                }
            },
            application_context = new
            {
                brand_name = "Mon Site E-commerce",
                locale = "sa-SA",
                shipping_preference = "NO_SHIPPING",
                user_action = "PAY_NOW",
                return_url = "https://localhost:44356/Success",
                cancel_url = "https://localhost:44356/Cancel",
                landing_page = "BILLING"
            },
            payment_source = new
            {
                card = new
                {
                    name = "John Doe",
                    billing_address = new
                    {
                        address_line_1 = "123 zemzem jubail",
                        admin_area_2 = "Dammam",
                        admin_area_1 = "jubail",
                        postal_code = "10999",
                        country_code = "SA"
                    }
                }
            }
        };

        var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync("https://api-m.sandbox.paypal.com/v2/checkout/orders", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Erreur PayPal: {response.StatusCode}";
            }

            var orderResponse = JsonConvert.DeserializeObject<PayPalOrderResponse>(result);
            if (orderResponse?.Links != null)
            {
                foreach (var link in orderResponse.Links)
                {
                    if (link != null && link.Rel == "approve" && !string.IsNullOrWhiteSpace(link.Href))
                    {
                        return link.Href;
                    }
                }
            }

            return "Erreur: L'URL d'approbation PayPal est manquante.";
        }
        catch (Exception ex)
        {
            return $"Erreur lors de la communication avec l'API PayPal: {ex.Message}";
        }
    }

    private async Task<string> GetAccessTokenAsync()
    {
        using var client = new HttpClient();
        var byteArray = Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        try
        {
            var response = await client.PostAsync("https://api.sandbox.paypal.com/v1/oauth2/token", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return string.Empty;

            var tokenResponse = JsonConvert.DeserializeObject<PayPalTokenResponse>(result);
            return tokenResponse?.AccessToken ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'obtention du token d'accès PayPal : {ex.Message}");
            return string.Empty;
        }
    }

    private class PayPalTokenResponse
    {
        [JsonProperty("access_token")]
        public string? AccessToken { get; set; }
    }

    private class PayPalOrderResponse
    {
        [JsonProperty("links")]
        public List<PayPalLink>? Links { get; set; }
    }

    private class PayPalLink
    {
        [JsonProperty("rel")]
        public string? Rel { get; set; }

        [JsonProperty("href")]
        public string? Href { get; set; }
    }
}
