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

    public int? OrderId { get; set; }

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

            var payment = new Payment
            {
                Id = uniquePaymentId,
                Name=user.UserName,
                CardNo = "1234567890123456",
                ExpiryDate = "12/25",
                CvvNo = 123,
                Address = "Client Address",
                PaymentMode = "CreditCard"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Payment created for PaymentId {uniquePaymentId}");

            int orderNo = new Random().Next(100000, 999999);
            Order? firstOrder = null;

            foreach (var cartProduct in lastCart.CartProducts)
            {
                var newOrder = new Order
                {
                    OrderNo = orderNo,
                    ProductId = cartProduct.ProductId,
                    Quantity = cartProduct.Quantity,
                    UserId = userId,
                    Status = "Paid",
                    PaymentId = uniquePaymentId,
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
