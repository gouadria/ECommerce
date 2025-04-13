using ECommerce.Models;
using ECommerce.Pages.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Pages
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private readonly EcommerceDbContext _context;
        private readonly ILogger<CategoryModel> _logger;

        // Propriétés pour stocker la liste des produits ; initialisées par défaut
        public IList<Product> Products { get; set; } = new List<Product>();
        public IList<Product> ProductsForEight { get; set; } = new List<Product>();

        // Propriété pour accepter les données du formulaire
        [BindProperty]
        public Product Product { get; set; } = new Product(); // Initialisé par défaut
        [BindProperty]
        public IFormFile ImageFile { get; set; }  // Pas d'erreur de non-nullabilité ici

        public IndexModel(EcommerceDbContext context, ILogger<CategoryModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task OnGetAsync(decimal total)
        {
            Amount = total;

            // Récupérer toutes les catégories
            var categories = await _context.Categories.ToListAsync();

            // Pour chaque catégorie, récupérer le premier produit disponible
            var Productsm = new List<Product>();

            foreach (var category in categories)
            {
                var product = await _context.Products
                    .Include(p => p.ProductImages)  // Inclure les images du produit
                    .Where(p => p.CategoryId == category.CategoryId && p.Quantity > 0)
                    .FirstOrDefaultAsync();

                if (product != null)
                {
                    Productsm.Add(product);  // Ajouter le produit à la liste
                }
            }

            // Limiter à 6 produits si nécessaire
            Products = Productsm.Take(6).ToList();

            ProductsForEight = await _context.Products
              .OrderBy(p => p.Category)
              .Include(p => p.Category)
              .Include(p => p.ProductImages)
              .Take(8)
              .ToListAsync();
        }

        // Propriété qui manque dans votre code original, ajoutée ici pour la compilation
        public decimal Amount { get; set; }
    }
}
