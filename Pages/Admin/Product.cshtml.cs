using ECommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Pages.Admin
{
    [Authorize(Roles = "ContactAdministrators, ContactManagers")]
    public class ProductModel : PageModel
    {
        private readonly EcommerceDbContext _context;
        private readonly ILogger<ProductModel> _logger;

        // Propriété pour stocker la liste des produits
        public IList<Product> Products { get; set; }

        // Propriété pour accepter les données du formulaire
        [BindProperty]
        public Product Product { get; set; }

        public SelectList CategoryList { get; set; }

        [BindProperty]
        public IFormFile[] ImageFile { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }

        [BindProperty(SupportsGet = true)]
        public string CategoryFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MinPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFilter { get; set; }

        public ProductModel(EcommerceDbContext context, ILogger<ProductModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Méthode OnGet pour afficher les produits avec les filtres
        public async Task OnGetAsync(int currentPage = 1)
        {
            try
            {
                var query = _context.Products.AsQueryable();

                // Filtrer par catégorie
                if (!string.IsNullOrEmpty(CategoryFilter))
                {
                    if (int.TryParse(CategoryFilter, out int categoryId))
                    {
                        query = query.Where(p => p.CategoryId == categoryId);
                    }
                }

                // Filtrer par prix
                if (MinPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= MinPrice.Value);
                }
                if (MaxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= MaxPrice.Value);
                }

                // Filtrer par date
                if (DateFilter.HasValue)
                {
                    query = query.Where(p => p.CreatedDate.Date == DateFilter.Value.Date);
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var pageSize = 5; // Nombre d'éléments par page
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                Products = await query
                    .OrderBy(p => p.ProductId)
                    .Include(p => p.ProductImages)
                    .Skip((currentPage - 1) * pageSize)
                    .Take(pageSize)
                    .Include(p => p.Category)
                    .ToListAsync();

                // Charger la liste des catégories
                CategoryList = new SelectList(_context.Categories, "CategoryId", "CategoryName");
                CurrentPage = currentPage;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la récupération des produits : {ex.Message}");
            }
        }

        // Méthode OnPostAsync pour ajouter un produit
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                _logger.LogInformation("Début de l'ajout du produit");

                Product.CreatedDate = DateTime.Now;
                Product.IsActive = Request.Form["IsActive"] == "on";

                _context.Products.Add(Product);
                await _context.SaveChangesAsync();

                var productImages = new List<ProductImage>();

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/AdminTemplate/assets/images");

                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    foreach (var file in ImageFile)
                    {
                        var fileExtension = Path.GetExtension(file.FileName).ToLower();

                        if (!validExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("", "L'image doit être de type JPG, JPEG, PNG ou GIF.");
                            return Page();
                        }

                        var fileName = Path.GetFileName(file.FileName);
                        var filePath = Path.Combine(directoryPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        productImages.Add(new ProductImage
                        {
                            ProductId = Product.ProductId,
                            ImageUrl = $"/AdminTemplate/assets/images/{fileName}",
                            DefaultImage = true
                        });
                    }

                    _context.ProductImages.AddRange(productImages);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Produit ajouté avec succès");
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de l'ajout du produit : {ex.Message}");
                ModelState.AddModelError("", "Une erreur est survenue lors de l'ajout du produit.");
                CategoryList = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName");
                return Page();
            }
        }

        // Méthode OnPostDeleteAsync pour supprimer un produit
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { currentPage = 1, CategoryFilter, MinPrice, MaxPrice, DateFilter });
        }

        // Méthode OnPostUpdateAsync pour mettre à jour un produit
        public async Task<IActionResult> OnPostUpdateAsync(int id, string name, bool isActive, IFormFile imageUrl)
        {
            if (string.IsNullOrEmpty(name))
            {
                ModelState.AddModelError("ProductName", "Le nom du produit ne peut pas être vide.");
                return Page();
            }

            try
            {
                var productToUpdate = await _context.Products
                                         .Include(p => p.ProductImages)  // Inclure les images associées
                                         .FirstOrDefaultAsync(p => p.ProductId == id);

                if (productToUpdate == null)
                {
                    return NotFound();
                }

                productToUpdate.ProductName = name;
                productToUpdate.IsActive = isActive;
                var productImages = new List<ProductImage>();

                if (imageUrl != null)
                {
                    var fileName = Path.GetFileName(imageUrl.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/AdminTemplate/assets/images", fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageUrl.CopyToAsync(stream);
                    }
                    
                    var imageUrlPath = $"/AdminTemplate/assets/images/{fileName}";

                    productImages.Add(new ProductImage
                    {
                        ProductId = productToUpdate.ProductId,
                        ImageUrl = imageUrlPath,
                        DefaultImage = true
                    });
                }
                _context.ProductImages.AddRange(productImages);

                await _context.SaveChangesAsync();
                return RedirectToPage(new { currentPage = 1, CategoryFilter, MinPrice, MaxPrice, DateFilter });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la mise à jour du produit avec ID {id}: {ex.Message}");
                ModelState.AddModelError("", "Une erreur est survenue lors de la mise à jour du produit.");
                return Page();
            }
        }
    }
}
