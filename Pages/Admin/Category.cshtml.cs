using Azure;
using ECommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public class CategoryModel : PageModel
    {
        private readonly EcommerceDbContext _context;
        private readonly ILogger<CategoryModel> _logger;

        // Initialisation avec valeurs par défaut pour éviter CS8618
        public IList<Category> Categories { get; set; } = new List<Category>();

        [BindProperty]
        public Category Category { get; set; } = new Category();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }

        public CategoryModel(EcommerceDbContext context, ILogger<CategoryModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task OnGetAsync(int currentPage = 1)
        {
            try
            {
                CurrentPage = currentPage < 1 ? 1 : currentPage;
                int totalRecords = await _context.Categories.CountAsync();
                int pageSize = 4;
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                Categories = await _context.Categories
                    .OrderBy(p => p.CategoryId)
                    .Skip((CurrentPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la récupération des catégories : {ex.Message}");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                _logger.LogInformation("Début de l'ajout de la catégorie");

                if (ImageFile != null)
                {
                    var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(ImageFile.FileName).ToLower();

                    if (!validExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("", "L'image doit être de type JPG, JPEG, PNG ou GIF.");
                        return Page();
                    }

                    var fileName = Path.GetFileName(ImageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/AdminTemplate/assets/images", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    Category.CategoryImageUrl = $"/AdminTemplate/assets/images/{fileName}";
                    _logger.LogInformation($"Image téléchargée : {Category.CategoryImageUrl}");
                }

                Category.CreatedDate = DateTime.Now;
                Category.IsActive = Request.Form["IsActive"] == "on";
                _context.Categories.Add(Category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Catégorie ajoutée avec succès");

                Categories = await _context.Categories.ToListAsync();
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de l'ajout de la catégorie : {ex.Message}");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync(int id, string Name, bool IsActive, IFormFile? ImageUrl)
        {
            var categoryToUpdate = await _context.Categories.FindAsync(id);
            if (categoryToUpdate == null)
            {
                return NotFound();
            }

            categoryToUpdate.CategoryName = Name;
            categoryToUpdate.IsActive = IsActive;

            if (ImageUrl != null && ImageUrl.Length > 0)
            {
                var fileName = Path.GetFileName(ImageUrl.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/AdminTemplate/assets/images", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageUrl.CopyToAsync(stream);
                }

                categoryToUpdate.CategoryImageUrl = $"/AdminTemplate/assets/images/{fileName}";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}
