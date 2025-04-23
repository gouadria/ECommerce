using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class Success1Model : PageModel
{
    private readonly EcommerceDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;

    public Success1Model(
        EcommerceDbContext context,
        UserManager<IdentityUser> userManager,
        IConfiguration configuration)
    {
        _context       = context;
        _userManager   = userManager;
        _configuration = configuration;
    }

    // Propriétés exposées à la Razor
    public string PaymentId     { get; private set; } = "";
    public string Status        { get; private set; } = "";
    public string PaymentSource { get; private set; } = "";
    public int    OrderId     { get; private set; }
    public int OrderNo { get; private set; }

    public async Task<IActionResult> OnGetAsync(
      [FromQuery(Name = "id")] string? paymentId,
    [FromQuery(Name = "status")] string? status,
    [FromQuery(Name = "payment_source")] string? paymentSource,
    [FromQuery(Name = "amount")] decimal? amount) 
    {
        // 1) Validation des paramètres obligatoires
        if (string.IsNullOrEmpty(paymentId)
         || string.IsNullOrEmpty(status)
         || string.IsNullOrEmpty(paymentSource))
        {
            TempData["ErrorMessage"] = "Paramètres de paiement manquants.";
            return RedirectToPage("/Error");
        }

        PaymentId     = paymentId!;
        Status        = status!.ToLowerInvariant();
        PaymentSource = paymentSource!;

        // 2) L'utilisateur doit être authentifié
        if (!User.Identity.IsAuthenticated)
        {
            TempData["ErrorMessage"] = "Vous devez être connecté pour finaliser le paiement.";
            return RedirectToPage("/Account/Login");
        }

        var user   = await _userManager.GetUserAsync(User);
        var userId = user?.Id;
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "Utilisateur introuvable.";
            return RedirectToPage("/Error");
        }

        // 3) Si Moyasar ET échec
        if (PaymentSource.Equals("Moyasar", StringComparison.OrdinalIgnoreCase)
         && (Status == "failed" || Status == "declined"))
        {
            TempData["ErrorMessage"] = $"Le paiement a échoué (statut = {Status}).";
            return RedirectToPage("/Error");
        }

        // 4) Si Moyasar ET attente de 3DS
        if (PaymentSource.Equals("Moyasar", StringComparison.OrdinalIgnoreCase)
         && Status == "initiated")
        {
            // On reste sur la page pour afficher un message du type "En attente de validation 3DS"
            return Page();
        }

        // 5) Si Moyasar ET statut payé/capturé → création de la commande
        if (PaymentSource.Equals("Moyasar", StringComparison.OrdinalIgnoreCase)
         && (Status == "paid" || Status == "captured"))
        {
            // Récupérer le panier en cours
            var lastCart = await _context.Carts
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedDate)
                .Include(c => c.CartProducts)
                    .ThenInclude(cp => cp.Product)
                .FirstOrDefaultAsync();

            if (lastCart == null || !lastCart.CartProducts.Any())
                return RedirectToPage("/Error");

            // 5.1) Enregistrer le paiement interne
            var payment = new Payment
            {
                Id          = Guid.NewGuid().ToString(),
                UserId      = userId,
                Name        = user.UserName,
                Address     = lastCart.CartProducts.First().Product.ProductName,
                PaymentMode = PaymentSource,
                Amount      = amount ?? 0m,
                CreatedDate = DateTime.UtcNow
            };
            _context.Payments.Add(payment);

            // 5.2) Créer les lignes de commande
            var orderNo = new Random().Next(100000, 999999);
            foreach (var cp in lastCart.CartProducts)
            {
                _context.Orders.Add(new Order
                {
                    OrderNo   = orderNo,
                    ProductId = cp.ProductId,
                    Quantity  = cp.Quantity,
                    UserId    = userId,
                    Status    = "Paid",
                    PaymentId = payment.Id,
                    OrderDate = DateTime.UtcNow,
                    IsConcel  = false
                });
            }

            await _context.SaveChangesAsync();

            // 5.3) Récupérer un OrderId pour l’affichage
            var firstOrder = await _context.Orders
                .Where(o => o.PaymentId == payment.Id)
                .OrderBy(o => o.OrderId)
                .FirstOrDefaultAsync();
            OrderId = firstOrder?.OrderId ?? 0;

            // 5.4) Vider le panier
            _context.Carts.Remove(lastCart);
            await _context.SaveChangesAsync();

            return Page();
        }

        // 6) Par défaut, on reste sur la page (cas improbable)
        return Page();
    }
}
