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


        // Propriété pour stocker la liste des catégories
        public IList<Product> Products { get; set; }

        public IList<Product> ProductsForEight { get; set; } // Pour 8 produits

        // Propriété pour accepter les données du formulaire
        [BindProperty]
        public Product Product { get; set; }
        [BindProperty]
        public IFormFile ImageFile { get; set; }
        
        public IndexModel(EcommerceDbContext context, ILogger<CategoryModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Méthode OnGet pour récupérer les catégories
        public async Task OnGetAsync()
        {
            // Étape 1 : Récupérer toutes les catégories
var categories = await _context.Categories
    .ToListAsync(); // Exécuter la requête pour récupérer toutes les catégories

// Étape 2 : Pour chaque catégorie, récupérer le premier produit disponible
var Productsm = new List<Product>();

foreach (var category in categories)
{
    var product = await _context.Products
        .Include(p => p.ProductImages)  // Inclure les images du produit
        .Where(p => p.CategoryId == category.CategoryId && p.Quantity > 0)  // Filtrer par catégorie et stock disponible
        .FirstOrDefaultAsync();  // Récupérer le premier produit pour cette catégorie

    if (product != null)
    {
        Productsm.Add(product);  // Ajouter le produit à la liste
    }
}

// Limiter à 6 produits si nécessaire
Products = Productsm.Take(6).ToList();

            ProductsForEight = await _context.Products
              .OrderBy(p => p.Category)
              .Include(p => p.Category) // Inclure la catégorie si nécessaire
              .Include(p => p.ProductImages)
              .Take(8)
              .ToListAsync();
        }
    }
}