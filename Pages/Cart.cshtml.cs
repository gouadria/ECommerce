using ECommerce.Models;
using Microsoft.AspNetCore.Antiforgery;
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
        private readonly IAntiforgery _antiforgery;

        public CartModel(EcommerceDbContext context, UserManager<IdentityUser> userManager, IAntiforgery antiforgery)
        {
            _context = context;
            _userManager = userManager;
            _antiforgery = antiforgery;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            // Vérifier si un panier est déjà enregistré dans la session
            var sessionCartId = HttpContext.Session.GetString("CartId");

            Cart cart;

            if (string.IsNullOrEmpty(sessionCartId))
            {
                // Création d'un nouveau panier
                cart = new Cart 
                { 
                    UserId = userId, 
                    CreatedDate = DateTime.UtcNow, 
                    CartProducts = new List<CartProduct>()
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

                // Stocker l'ID du panier dans la session
                HttpContext.Session.SetString("CartId", cart.CartId.ToString());
            }
            else
            {
                // Récupération du panier via l'ID stocké en session
                cart = await _context.Carts
                    .Include(c => c.CartProducts)
                    .FirstOrDefaultAsync(c => c.CartId.ToString() == sessionCartId);

                if (cart == null)
                {
                    cart = new Cart 
                    { 
                        UserId = userId, 
                        CreatedDate = DateTime.UtcNow, 
                        CartProducts = new List<CartProduct>()
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetString("CartId", cart.CartId.ToString());
                }
            }

            // Passer le panier à la vue et préparer le token CSRF
            ViewData["Cart"] = cart;
            ViewData["CsrfToken"] = _antiforgery.GetTokens(HttpContext).RequestToken;

            return Page();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
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
                Console.WriteLine("Données du panier reçues : " + Newtonsoft.Json.JsonConvert.SerializeObject(cartItems));

                var existingCart = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.CreatedDate)
                    .FirstOrDefaultAsync();

                if (existingCart == null)
                {
                    existingCart = new Cart 
                    { 
                        UserId = userId, 
                        CreatedDate = DateTime.UtcNow, 
                        CartProducts = new List<CartProduct>()
                    };
                    _context.Carts.Add(existingCart);
                    await _context.SaveChangesAsync();
                }

                foreach (var item in cartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                    {
                        Console.WriteLine($"Produit avec ID {item.ProductId} non trouvé.");
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
                            // Nous n'initialisons pas ici les propriétés de navigation (Cart et Product)
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
