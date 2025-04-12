using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Linq;

public class PayPalService
{
    private readonly string clientId = "AZiyBCUZdE9FH052NsjzV6WR7Q8hferrf6jefF8aAsw7wN_HWFaiNI3cFKW7PKYl2unBnrL-nm5vAXh6";
    private readonly string clientSecret = "EH-sF2WEEDHOneYXchOH74jAXUZb0vVjyGR34bqgjJXRb-bX725c2IYOFBExs1wI_mz3AS9Fljjya-oF";

    public async Task<string> CreatePayPalOrderAsync(decimal amount, string email, string phone)
    {
        var accessToken = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            return "Erreur: impossible d'obtenir le token d'accès.";
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
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

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                dynamic jsonResponse = JsonConvert.DeserializeObject(result);
                IEnumerable<dynamic>? links = jsonResponse?.links;

                if (links != null)
                {
                    string? approvalUrl = links.ElementAtOrDefault(1)?.href;
                    return approvalUrl ?? "Erreur: L'URL d'approbation PayPal est manquante.";
                }
                else
                {
                    return "Erreur: Les liens de la réponse PayPal sont manquants.";
                }
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                return $"Erreur lors de la création du paiement: {errorResponse}";
            }
        }
        catch (Exception ex)
        {
            return $"Erreur lors de la communication avec l'API PayPal: {ex.Message}";
        }
    }

    private async Task<string> GetAccessTokenAsync()
    {
        using var client = new HttpClient();
        var byteArray = new UTF8Encoding().GetBytes($"{clientId}:{clientSecret}");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await client.PostAsync("https://api.sandbox.paypal.com/v1/oauth2/token", content);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            dynamic jsonResponse = JsonConvert.DeserializeObject(result);
            string? token = jsonResponse?.access_token;
            return token ?? string.Empty;
        }

        return string.Empty;
    }
}
