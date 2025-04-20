using ECommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Pages
{
    [Authorize]
    public class CheckoutModel : PageModel
    {
        private readonly EcommerceDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CheckoutModel(EcommerceDbContext context, UserManager<IdentityUser> userManager)
        {
            _context     = context;
            _userManager = userManager;
        }

        public List<CartProduct> CartProducts { get; set; } = new();
        public decimal          TotalAmount  { get; set; }
        public IdentityUser     CurrentUser  { get; set; }

        [BindProperty] public string FullName { get; set; }
        [BindProperty] public string Email    { get; set; }
        [BindProperty] public string Phone    { get; set; }
        [BindProperty] public string Address  { get; set; }

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
    }
}

