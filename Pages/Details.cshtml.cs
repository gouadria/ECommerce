using ECommerce.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Pages
{
    [AllowAnonymous]
    public class DetailsModel : PageModel
    {
        private readonly EcommerceDbContext _context;

        // Propriété pour stocker le produit
        public Product Product { get; set; }

        // Liste pour stocker les images du produit
        public IList<ProductImage> ProductImages { get; set; }

        public DetailsModel(EcommerceDbContext context)
        {
            _context = context;
        }

        // Cette méthode est appelée lors de la requête GET
        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Récupérer le produit avec ses images associées
            var product = await _context.Products
                .OrderBy(p=> p.ProductId)
                .Include(p => p.ProductImages)  // Inclure les images associées au produit
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();  // Si le produit n'est pas trouvé
            }

            Product = product;  // Affecter le produit récupéré
            ProductImages = product.ProductImages.ToList();  // Récupérer la liste des images

            return Page();
        }
    }
}

