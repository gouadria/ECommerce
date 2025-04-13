using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ECommerce.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Pages
{
    [IgnoreAntiforgeryToken] // Désactive temporairement CSRF pour tester si le problème vient de là
    [Route("/Payement1")]
    public class Payement1Model : PageModel
    {
        private readonly RazorpayService _razorpayService;
        private readonly IConfiguration _configuration;
        private readonly UserManager<IdentityUser> _userManager;

        [BindProperty] public decimal Amount { get; set; }
        [BindProperty] public string? PhoneNumber { get; set; }
        public string? UserEmail { get; set; }


       public Payement1Model(IConfiguration configuration, UserManager<IdentityUser> userManager, RazorpayService razorpayService)
{
    _configuration = configuration;
    _razorpayService = razorpayService;
    _userManager = userManager;
    Amount = 50.00M;
    PhoneNumber = string.Empty;
    UserEmail = string.Empty;
}

        public async Task<IActionResult> OnGetAsync(decimal? total, string email, string phone)
        {
            Console.WriteLine($"QueryString: {Request.QueryString}");
            Console.WriteLine($"Total: {total}, Email: {email}, Phone: {phone}");

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phone))
            {
                Console.WriteLine("❌ Erreur: Email ou numéro de téléphone manquant.");
                return BadRequest("Email et numéro de téléphone obligatoires.");
            }

            Amount = total ?? 50.00M;
            UserEmail = email;
            PhoneNumber = phone;

            ViewData["RazorpayKey"] = _configuration["Razorpay:KeyId"];
            return Page();
        }

        [HttpPost]
        public async Task<IActionResult> OnPostAsync([FromBody] PaymentRequest paymentRequest)
        {
            if (paymentRequest == null || paymentRequest.Amount <= 0)
            {
                Console.WriteLine("Montant invalide reçu : " + paymentRequest?.Amount);
                return BadRequest("Montant invalide.");
            }

            decimal amountInInr = ConvertSarToInr(paymentRequest.Amount);
            Console.WriteLine($"🛠 Requête reçue: {JsonSerializer.Serialize(paymentRequest)}");


            try
            {
                var orderResponse = await _razorpayService.CreateRazorpayOrderAsync(amountInInr, paymentRequest.Email, paymentRequest.PhoneNumber);

                using var jsonDoc = JsonDocument.Parse(orderResponse);
                if (jsonDoc.RootElement.TryGetProperty("id", out var orderId))
                {
                    return new JsonResult(new
                    {
                        orderId = orderId.GetString(),
                        amount = amountInInr,
                        currency = "INR",
                        email = paymentRequest.Email,
                        phoneNumber = paymentRequest.PhoneNumber,
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }

                Console.WriteLine("❌ Erreur : Réponse de Razorpay invalide.");
                return BadRequest("Erreur lors de la création de la commande Razorpay.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception Razorpay : {ex.Message}");

                var errorResponse = new
                {
                    error = "Erreur interne lors de la création de la commande.",
                    details = ex.Message
                };

                Response.ContentType = "application/json"; // 🔥 Forcer la réponse en JSON
                return new JsonResult(errorResponse) { StatusCode = 500 };
            }

        }

        private decimal ConvertSarToInr(decimal amountInSar)
        {
            decimal conversionRate = 23.34M;
            return amountInSar * conversionRate;
        }
    }

    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public string Email { get; set; }
        [JsonPropertyName("phone")]
        public string PhoneNumber { get; set; }
    }
}





