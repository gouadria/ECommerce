using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ECommerce.Models;

public class OrdersModel : PageModel
{
    private readonly EcommerceDbContext _context;

    public OrdersModel(EcommerceDbContext context)
    {
        _context = context;
    }

    public IList<Order> Orders { get; set; }

    public async Task OnGetAsync()
    {
        Orders = await _context.Orders
        .OrderByDescending(o => o.OrderDate)  // Tri d�croissant : le plus r�cent d'abord
        .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(int[] processedOrders)
    {
        // R�cup�rer les commandes marqu�es pour mise � jour
        if (processedOrders != null)
        {
            var ordersToUpdate = await _context.Orders
                .Where(o => processedOrders.Contains(o.OrderId))
                .ToListAsync();

            foreach (var order in ordersToUpdate)
            {
                // Mettre � jour le statut en fonction de la case � cocher
                order.Status = "Trait�";  // On marque comme trait� si la case est coch�e
            }

            await _context.SaveChangesAsync();  // Sauvegarder les changements dans la base de donn�es
        }

        // Rediriger vers la m�me page pour actualiser la liste des commandes
        return RedirectToPage();
    }
}
