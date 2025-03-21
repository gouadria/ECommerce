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
    public class CheckoutModel : PageModel
    {
        private readonly EcommerceDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CheckoutModel(EcommerceDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<CartProduct> CartProducts { get; set; }
        public decimal TotalAmount { get; set; }
        public IdentityUser CurrentUser { get; set; }

        public async Task<IActionResult> OnGetAsync(string total, string username, string email, string phone)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            CurrentUser = await _userManager.FindByIdAsync(userId);

            var lastCart = await _context.Carts
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedDate)
                .Include(c => c.CartProducts)
                .ThenInclude(cp => cp.Product)
                .FirstOrDefaultAsync();

            if (lastCart != null)
            {
                CartProducts = lastCart.CartProducts;
                TotalAmount = CartProducts.Sum(cp => cp.Product.Price * cp.Quantity);
            }
            else
            {
                CartProducts = new List<CartProduct>();
                TotalAmount = 0;
            }
            Console.WriteLine($"Total: {total}, User: {username}, Email: {email}, Phone: {phone}");

            return Page();
        }
    }
}



