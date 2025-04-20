using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ECommerce.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Pages.Admin
{
    [Authorize(Roles = "ContactAdministrators, ContactManagers")]
    public class OrdersModel : PageModel
    {
        private readonly EcommerceDbContext _context;
        public OrdersModel(EcommerceDbContext context) => _context = context;

        public List<Order> Orders { get; set; } = new();

        public async Task OnGetAsync()
        {
            Orders = await _context.Orders
                .Include(o => o.User)                       // ← on inclut le User
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync(int[] processedOrders)
        {
            if (processedOrders?.Any() == true)
            {
                var toUpdate = await _context.Orders
                    .Where(o => processedOrders.Contains(o.OrderId))
                    .ToListAsync();
                foreach (var o in toUpdate) o.Status = "Traité";
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}

