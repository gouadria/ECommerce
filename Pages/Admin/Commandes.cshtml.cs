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
        .OrderByDescending(o => o.OrderDate)  // Tri décroissant : le plus récent d'abord
        .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(int[] processedOrders)
    {
        // Récupérer les commandes marquées pour mise à jour
        if (processedOrders != null)
        {
            var ordersToUpdate = await _context.Orders
                .Where(o => processedOrders.Contains(o.OrderId))
                .ToListAsync();

            foreach (var order in ordersToUpdate)
            {
                // Mettre à jour le statut en fonction de la case à cocher
                order.Status = "Traité";  // On marque comme traité si la case est cochée
            }

            await _context.SaveChangesAsync();  // Sauvegarder les changements dans la base de données
        }

        // Rediriger vers la même page pour actualiser la liste des commandes
        return RedirectToPage();
    }
}
