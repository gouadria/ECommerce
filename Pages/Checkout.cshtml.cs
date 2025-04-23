using ECommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json; // Pour JsonDocument
using System;
using System.Text;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;


namespace ECommerce.Pages
{
    [Authorize]
    public class CheckoutModel : PageModel
    {
        private readonly EcommerceDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public CheckoutModel(EcommerceDbContext context, UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        public List<CartProduct> CartProducts { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public IdentityUser CurrentUser { get; set; }

        [BindProperty] public string FullName { get; set; }
        [BindProperty] public string Email { get; set; }
        [BindProperty] public string Phone { get; set; }
        [BindProperty] public string Address { get; set; }
        [BindProperty]
         public CheckoutRequestDto CheckoutInfo { get; set; }
         
          public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Identity/Account/Login");

            CurrentUser = await _userManager.FindByIdAsync(userId);
            FullName    = CurrentUser.UserName ?? "";
            Email       = CurrentUser.Email    ?? "";

            var lastCart = await _context.Carts
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedDate)
                .Include(c => c.CartProducts).ThenInclude(cp => cp.Product)
                .FirstOrDefaultAsync();

            if (lastCart != null)
            {
                CartProducts = lastCart.CartProducts;
                TotalAmount  = CartProducts.Sum(cp => cp.Product.Price * cp.Quantity);
            }

            return Page();
        }

        // DTO inchangé
        public class PayPalDto
        {
            public string? orderID  { get; set; }
            public string? payerID  { get; set; }
            public string? fullName { get; set; }
            public string? address  { get; set; }
            public string? phone    { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostPayPalAsync([FromBody] PayPalDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return new JsonResult(new { success = false, message = "Utilisateur non connecté." });

            var cart = await _context.Carts
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedDate)
                .Include(c => c.CartProducts).ThenInclude(cp => cp.Product)
                .FirstOrDefaultAsync();

            if (cart == null || !cart.CartProducts.Any())
                return new JsonResult(new { success = false, message = "Panier vide." });

            // ← On inclut maintenant BOTH orderID et payerID dans l’URL de succès
            var successUrl = Url.Page(
                "/Success",
                values: new { token   = dto.orderID,
                               payerId = dto.payerID }
            );

            return new JsonResult(new { success = true, redirectUrl = successUrl });
        }
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> OnPostMoyossarCheckoutAsync([FromBody] CheckoutRequestDto request)
{
    // 1) URL API Moyasar
    var apiUrl = _configuration["Moyasar:ApiUrl"];
    if (string.IsNullOrWhiteSpace(apiUrl))
        throw new InvalidOperationException("La configuration 'Moyasar:ApiUrl' est manquante.");

    using var client = new HttpClient();
    var secret    = _configuration["Moyasar:SecretKey"];
    var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secret}:"));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    if (!decimal.TryParse(request.Amount, out var montantDecimal))
        return new JsonResult(new { success = false, message = "Montant invalide." });
    var amountInHalala = (int)(montantDecimal * 100);

    var cleanName = Regex.Replace(request.FullName ?? "", @"[^A-Za-z\s]", "").Trim();

    // 2) Créer le paiement Moyasar
    var payload = new
    {
        amount      = amountInHalala,
        currency    = "SAR",
        description = "Commande E‑commerce",
        callback_url = "https://bayacommerce-bxbzb0fnf9h0dkff.westeurope-01.azurewebsites.net/Success1?payment_source=Moyasar",
        source = new
        {
            type   = "creditcard",
            name   = cleanName,
            number = request.CardNumber,
            cvc    = request.CVC,
            month  = request.ExpiryMonth,
            year   = request.ExpiryYear
        }
    };

    var content  = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    var response = await client.PostAsync(apiUrl, content);
    var body     = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
        return new JsonResult(new { success = false, message = "Erreur Moyasar : " + body });

    using var doc = JsonDocument.Parse(body);

    // 3) On récupère le transaction_url pour le 3DS
    if (doc.RootElement.TryGetProperty("source", out var sourceElem) &&
        sourceElem.TryGetProperty("transaction_url", out var txUrlElem))
    {
        var transactionUrl = txUrlElem.GetString();
        if (string.IsNullOrEmpty(transactionUrl))
            return new JsonResult(new { success = false, message = "transaction_url manquant." });

        return new JsonResult(new { success = true, redirectUrl = transactionUrl });
    }

    return new JsonResult(new { success = false, message = "transaction_url introuvable." });
}

 public class CheckoutRequestDto
        {
            public string? Amount { get; set; }
            public string? FullName { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? Address { get; set; }
            public string? CardNumber { get; set; }
            public string? CVC { get; set; }
            public string? ExpiryMonth { get; set; }
            public string? ExpiryYear { get; set; }
        }

    }
    }
