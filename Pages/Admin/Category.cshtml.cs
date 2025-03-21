using Azure;
using ECommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
        
        
        // Propri�t� pour stocker la liste des cat�gories
        public IList<Category> Categories { get; set; }
        

        // Propri�t� pour accepter les donn�es du formulaire
        [BindProperty]
        public Category Category { get; set; }
        [BindProperty]
        public IFormFile ImageFile { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public CategoryModel(EcommerceDbContext context, ILogger<CategoryModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // M�thode OnGet pour r�cup�rer les cat�gories
        public async Task OnGetAsync(int currentPage = 1)
        {
            try
            {
                
                CurrentPage = currentPage < 1 ? 1 : currentPage;
                int TotalRecords = await _context.Categories.CountAsync();
                int pageSize = 4;
                TotalPages = (int)Math.Ceiling(TotalRecords / (double)pageSize);
               
                Categories = await _context.Categories
                              .OrderBy(p => p.CategoryId)
                              .Skip((CurrentPage - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la r�cup�ration des cat�gories : {ex.Message}");
            }
        }

        // M�thode OnPostAsync pour ajouter une cat�gorie
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                _logger.LogInformation("D�but de l'ajout de la cat�gorie");
                
                if (ImageFile != null)
                {
                    var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(ImageFile.FileName).ToLower();

                    if (!validExtensions.Contains(fileExtension))
                    {
                        // Ajouter une erreur de validation si l'extension est invalide
                        ModelState.AddModelError("", "L'image doit �tre de type JPG, JPEG, PNG ou GIF.");
                        return Page(); // Renvoyer la page avec l'erreur
                    }

                    // G�rer le t�l�chargement de l'image
                    var fileName = Path.GetFileName(ImageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/AdminTemplate/assets/images", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    // Stocker le chemin relatif correct
                    Category.CategoryImageUrl = $"/AdminTemplate/assets/images/{fileName}";
                    _logger.LogInformation($"Image t�l�charg�e : {Category.CategoryImageUrl}");
                }

                Category.CreatedDate = DateTime.Now;
                Category.IsActive = Request.Form["IsActive"] == "on";
                _context.Categories.Add(Category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cat�gorie ajout�e avec succ�s");

                Categories = await _context.Categories.ToListAsync();

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de l'ajout de la cat�gorie : {ex.Message}");
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
        public async Task<IActionResult> OnPostUpdateAsync(int id, string Name, bool IsActive, IFormFile ImageUrl)
        {
            var categoryToUpdate = await _context.Categories.FindAsync(id);

            if (categoryToUpdate == null)
            {
                return NotFound();
            }

            // Mise � jour des autres champs
            categoryToUpdate.CategoryName = Name;
            categoryToUpdate.IsActive = Request.Form["IsActive"] == "on";  // Utilisez directement la valeur de IsActive

            // Gestion de l'image
            if (ImageUrl != null && ImageUrl.Length > 0)
            {
                // Cr�ez un nom de fichier unique pour �viter les conflits
                var fileName = Path.GetFileName(ImageUrl.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "AdminTemplate", "assets", "images", fileName);

                // Enregistrer l'image dans le dossier images
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageUrl.CopyToAsync(stream);
                }

                // Mettre � jour l'URL de l'image dans la base de donn�es (chemin relatif)
                categoryToUpdate.CategoryImageUrl = $"/AdminTemplate/assets/images/{fileName}";
            }

            // Sauvegarder les changements
            await _context.SaveChangesAsync();

            // Rediriger vers la page apr�s la mise � jour
            return RedirectToPage();
        }

    }

}

