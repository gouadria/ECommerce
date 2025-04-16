using ECommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Pages
{
    [Authorize]
    public class CartModel : PageModel
    {
        private readonly EcommerceDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CartModel(EcommerceDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var sessionCartId = HttpContext.Session.GetString("CartId");
            Cart cart;

            if (string.IsNullOrEmpty(sessionCartId))
            {
                cart = new Cart { UserId = userId, CreatedDate = DateTime.UtcNow, CartProducts = new List<CartProduct>() };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("CartId", cart.CartId.ToString());
            }
            else
            {
                cart = await _context.Carts
                    .Include(c => c.CartProducts)
                    .FirstOrDefaultAsync(c => c.CartId.ToString() == sessionCartId);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId, CreatedDate = DateTime.UtcNow, CartProducts = new List<CartProduct>() };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetString("CartId", cart.CartId.ToString());
                }
            }

            ViewData["Cart"] = cart;
            return Page();
        }

        [IgnoreAntiforgeryToken] // Important pour requêtes AJAX JSON
        public async Task<IActionResult> OnPostSaveCartAsync([FromBody] List<CartItemDto> cartItems)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            if (cartItems == null || !cartItems.Any())
            {
                return BadRequest(new { success = false, message = "Le panier est vide." });
            }

            try
            {
                var existingCart = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.CreatedDate)
                    .FirstOrDefaultAsync();

                if (existingCart == null)
                {
                    existingCart = new Cart { UserId = userId, CreatedDate = DateTime.UtcNow, CartProducts = new List<CartProduct>() };
                    _context.Carts.Add(existingCart);
                    await _context.SaveChangesAsync();
                }

                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                    {
                        continue;
                    }

                    var existingProductInCart = await _context.CartProducts
                        .FirstOrDefaultAsync(cp => cp.CartId == existingCart.CartId && cp.ProductId == item.ProductId);

                    if (existingProductInCart != null)
                    {
                        existingProductInCart.Quantity += item.Quantity;
                    }
                    else
                    {
                        _context.CartProducts.Add(new CartProduct
                        {
                            CartId = existingCart.CartId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            ProductName = product.ProductName
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, message = "Panier enregistré avec succès !" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'enregistrement du panier : {ex.Message}");
                return StatusCode(500, new { success = false, message = "Erreur interne du serveur." });
            }
        }

        public class CartItemDto
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }
    }
}
