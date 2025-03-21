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

        // Propri�t� pour stocker le produit
        public Product Product { get; set; }

        // Liste pour stocker les images du produit
        public IList<ProductImage> ProductImages { get; set; }

        public DetailsModel(EcommerceDbContext context)
        {
            _context = context;
        }

        // Cette m�thode est appel�e lors de la requ�te GET
        public async Task<IActionResult> OnGetAsync(int id)
        {
            // R�cup�rer le produit avec ses images associ�es
            var product = await _context.Products
                .OrderBy(p=> p.ProductId)
                .Include(p => p.ProductImages)  // Inclure les images associ�es au produit
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();  // Si le produit n'est pas trouv�
            }

            Product = product;  // Affecter le produit r�cup�r�
            ProductImages = product.ProductImages.ToList();  // R�cup�rer la liste des images

            return Page();
        }
    }
}

