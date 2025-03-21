using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class SuccessModel : PageModel
{
    private readonly EcommerceDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public SuccessModel(EcommerceDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public int OrderId { get; set; }

    public async Task<IActionResult> OnGetAsync(string token, string payerId)
    {
        Console.WriteLine($"Success Page Called - Token: {token}, PayerId: {payerId}");

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine("User not authenticated, redirecting to login.");
            return RedirectToPage("/Account/Login");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            Console.WriteLine("User not found, redirecting to login.");
            return RedirectToPage("/Account/Login");
        }

        if (string.IsNullOrEmpty(payerId) || string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Invalid Payment details, redirecting to error.");
            TempData["ErrorMessage"] = "Invalid Payment details.";
            return RedirectToPage("/Error");
        }

        // Récupérer le dernier panier
        var lastCart = await _context.Carts
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedDate)
            .Include(c => c.CartProducts)
            .ThenInclude(cp => cp.Product)
            .FirstOrDefaultAsync();

        if (lastCart == null || !lastCart.CartProducts.Any())
        {
            Console.WriteLine("No valid cart found, redirecting to error.");
            TempData["ErrorMessage"] = "Your cart is empty or missing.";
            return RedirectToPage("/Error");
        }

        Console.WriteLine($"Cart found: {lastCart.CartId}, Products count: {lastCart.CartProducts.Count}");

        try
        {
            var uniquePaymentId = Guid.NewGuid().ToString();
            // Créer un paiement avec payerId comme PaymentId
            var payment = new Payment
            {
                PaymentId = uniquePaymentId, // Utilisation de payerId comme PaymentId
                Name = "Client Name", // Remplacer par le nom réel du client
                CardNo = "1234567890123456", // Remplacer par le numéro de carte réel (en masquant si nécessaire)
                ExpiryDate = "12/25", // Remplacer par la date d'expiration réelle
                CvvNo = 123, // Remplacer par le CVV réel (en masquant si nécessaire)
                Address = "Client Address", // Remplacer par l'adresse réelle
                PaymentMode = "CreditCard", // ou "PayPal", selon le mode de paiement utilisé
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Payment created for PaymentId {payerId}");

            // Générer un numéro de commande unique
            int orderNo = new Random().Next(100000, 999999);
            Order firstOrder = null;

            foreach (var cartProduct in lastCart.CartProducts)
            {
                var newOrder = new Order
                {
                    OrderNo = orderNo,
                    ProductId = cartProduct.ProductId,
                    Quantity = cartProduct.Quantity,
                    UserId = userId,
                    Status = "Paid",
                    PaymentId = uniquePaymentId,  // Utilisation du payerId comme PaymentId
                    OrderDate = DateTime.UtcNow,
                    IsConcel = false
                };

                _context.Orders.Add(newOrder);
                Console.WriteLine($"Order added: {newOrder.ProductId}, Quantity: {newOrder.Quantity}");

                if (firstOrder == null)
                {
                    firstOrder = newOrder;
                }
            }

            await _context.SaveChangesAsync();

            if (firstOrder != null)
            {
                OrderId = firstOrder.OrderId;
                Console.WriteLine($"First OrderId assigned: {OrderId}");
            }

            // Vider le panier après validation
            Console.WriteLine($"Removing cart: {lastCart.CartId}");
            _context.Carts.Remove(lastCart);
            await _context.SaveChangesAsync();

            Console.WriteLine("Cart removed successfully.");
            return Page();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing order: {ex.Message}");
            TempData["ErrorMessage"] = "An error occurred while processing your order.";
            return RedirectToPage("/Error");
        }
    }

}
