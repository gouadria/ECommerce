using ECommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Pages
{
    [Authorize]
    public class CartModel : PageModel
    {
        private readonly EcommerceDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CartModel(EcommerceDbContext context, UserManager<IdentityUser> userManager)
        {
            _context     = context;
            _userManager = userManager;
        }

        // Chargement / création du panier
        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToPage("/Account/Login");

            var sessionId = HttpContext.Session.GetString("CartId");
            Cart cart = null;

            if (int.TryParse(sessionId, out var cid))
            {
                cart = await _context.Carts
                    .Include(c => c.CartProducts)
                    .FirstOrDefaultAsync(c => c.CartId == cid && c.UserId == userId);
            }

            if (cart == null)
            {
                cart = new Cart {
                  UserId      = userId,
                  CreatedDate = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            HttpContext.Session.SetString("CartId", cart.CartId.ToString());
            ViewData["Cart"] = cart;
            return Page();
        }

        public class CartItemDto
        {
            public int ProductId { get; set; }
            public int Quantity  { get; set; }
        }

        // Handler qui reçoit [{ productId, quantity }, …]
        [IgnoreAntiforgeryToken]
        public async Task<JsonResult> OnPostSaveCartAsync([FromBody] List<CartItemDto> items)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return new JsonResult(new { success = false, message = "Auth requise" });

            if (items == null || items.Count == 0)
                return new JsonResult(new { success = false, message = "Panier vide" });

            if (!int.TryParse(HttpContext.Session.GetString("CartId"), out var cartId))
                return new JsonResult(new { success = false, message = "Session invalide" });

            var cart = await _context.Carts
                .Include(c => c.CartProducts)
                .FirstOrDefaultAsync(c => c.CartId == cartId && c.UserId == userId);

            if (cart == null)
                return new JsonResult(new { success = false, message = "Panier introuvable" });

            // Vider l'ancien contenu
            _context.CartProducts.RemoveRange(cart.CartProducts);
            await _context.SaveChangesAsync();

            // Ajouter les nouvelles lignes
            foreach (var dto in items)
            {
                if (dto.ProductId <= 0) continue;
                var prod = await _context.Products.FindAsync(dto.ProductId);
                if (prod == null) continue;

                _context.CartProducts.Add(new CartProduct {
                  CartId      = cart.CartId,
                  ProductId   = prod.ProductId,
                  Quantity    = dto.Quantity,
                  ProductName = prod.ProductName
                });
            }

            await _context.SaveChangesAsync();

            // On renvoie l'URL de Checkout
            var url = Url.Page("/Checkout");
            return new JsonResult(new { success = true, redirectTo = url });
        }
    }
}
