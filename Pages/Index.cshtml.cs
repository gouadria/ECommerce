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


        // Propri�t� pour stocker la liste des cat�gories
        public IList<Product> Products { get; set; }

        public IList<Product> ProductsForEight { get; set; } // Pour 8 produits

        // Propri�t� pour accepter les donn�es du formulaire
        [BindProperty]
        public Product Product { get; set; }
        [BindProperty]
        public IFormFile ImageFile { get; set; }
        
        public IndexModel(EcommerceDbContext context, ILogger<CategoryModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // M�thode OnGet pour r�cup�rer les cat�gories
        public async Task OnGetAsync()
        {
            // �tape 1 : R�cup�rer toutes les cat�gories
var categories = await _context.Categories
    .ToListAsync(); // Ex�cuter la requ�te pour r�cup�rer toutes les cat�gories

// �tape 2 : Pour chaque cat�gorie, r�cup�rer le premier produit disponible
var Productsm = new List<Product>();

foreach (var category in categories)
{
    var product = await _context.Products
        .Include(p => p.ProductImages)  // Inclure les images du produit
        .Where(p => p.CategoryId == category.CategoryId && p.Quantity > 0)  // Filtrer par cat�gorie et stock disponible
        .FirstOrDefaultAsync();  // R�cup�rer le premier produit pour cette cat�gorie

    if (product != null)
    {
        Productsm.Add(product);  // Ajouter le produit � la liste
    }
}

// Limiter � 6 produits si n�cessaire
Products = Productsm.Take(6).ToList();

            ProductsForEight = await _context.Products
              .OrderBy(p => p.Category)
              .Include(p => p.Category) // Inclure la cat�gorie si n�cessaire
              .Include(p => p.ProductImages)
              .Take(8)
              .ToListAsync();
        }
    }
}