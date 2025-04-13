using System.Linq;
using System.Threading.Tasks;
using ECommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

public class InvoiceModel : PageModel
{
    private readonly EcommerceDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    // ✅ Plus besoin du "?" car on initialise avec une liste vide
    public List<Order> Orders { get; set; } = new();
    public string UserName { get; set; } = string.Empty;

    public InvoiceModel(EcommerceDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync(int orderId)
    {
        Console.WriteLine($"Fetching invoice for orderId: {orderId}");

        if (orderId == 0) return RedirectToPage("/Error");

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Account/Login");

        UserName = user.UserName ?? string.Empty;


        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == user.Id);

        if (order == null) return RedirectToPage("/Error");

        Orders = await _context.Orders
            .Where(o => o.OrderNo == order.OrderNo && o.UserId == user.Id)
            .Include(o => o.Product)
            .ToListAsync();

        // ✅ Ce check est désormais facultatif car Orders ne sera jamais null,
        // mais on garde le test logique
        if (!Orders.Any()) return RedirectToPage("/Error");

        return Page();
    }
}





