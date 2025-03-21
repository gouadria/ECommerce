#nullable disable

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace ECommerce.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ZohoEmailService _sender;
        private readonly ILogger<RegisterConfirmationModel> _logger;
        public RegisterConfirmationModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ZohoEmailService sender, ILogger<RegisterConfirmationModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _sender = sender;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public bool DisplayConfirmAccountLink { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string EmailConfirmationUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string email, string returnUrl = null)
        {
            // Vérifier si userId ou email est fourni
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(email))
            {
                _logger.LogError("UserId ou email doit être fourni dans l'URL.");
                return BadRequest("UserId ou email doit être fourni dans l'URL.");
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            IdentityUser user = null;

            // Si userId est fourni, récupérer l'utilisateur par userId
            if (!string.IsNullOrEmpty(userId))
            {
                user = await _userManager.FindByIdAsync(userId);
            }
            // Si userId est manquant, récupérer l'utilisateur par email
            else if (!string.IsNullOrEmpty(email))
            {
                user = await _userManager.FindByEmailAsync(email);
            }

            if (user == null)
            {
                _logger.LogError($"Aucun utilisateur trouvé avec l'ID '{userId}' ou l'email '{email}'.");
                return NotFound($"Impossible de charger l'utilisateur avec l'ID '{userId}' ou l'email '{email}'.");
            }

            Email = user.Email;

            // Vérifier si l'email est déjà confirmé
            if (user.EmailConfirmed)
            {
                // Si l'email est déjà confirmé, rediriger vers la page de succès ou la page d'accueil
                _logger.LogInformation($"L'email de l'utilisateur '{user.Email}' est déjà confirmé.");
                return RedirectToPage("/Account/ConfirmationSuccess", new { returnUrl });
            }
            else
            {
                // Si l'email n'est pas encore confirmé, générer le lien de confirmation
                DisplayConfirmAccountLink = true;

                // Générer un code de confirmation d'email
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code)); // Encodage du code pour l'URL

                // Générer le lien de confirmation
                EmailConfirmationUrl = Url.Page(
                    "/Account/ConfirmEmail", // Page de confirmation
                    pageHandler: null,
                    values: new { area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl },
                    protocol: Request.Scheme); // Utiliser le protocole (HTTP ou HTTPS) en fonction de l'environnement

                // Envoi du lien de confirmation par email (ici, à implémenter selon votre logique d'email)
            }

            return Page();
        }






        // Cette méthode est pour gérer la confirmation de l'email
        public async Task<IActionResult> OnGetConfirmEmailAsync(string userId, string code, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            {
                _logger.LogError("User ID ou code est null.");
                return BadRequest("User ID ou code est null.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError($"Unable to load user with ID '{userId}'.");
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            try
            {
                var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                var result = await _userManager.ConfirmEmailAsync(user, decodedCode);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User with ID '{userId}' confirmed successfully.");
                    user.EmailConfirmed = true;

                    var updateResult = await _userManager.UpdateAsync(user);
                    if (updateResult.Succeeded)
                    {
                        _logger.LogInformation($"User with ID '{userId}' updated successfully.");
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToPage("/Account/ConfirmationSuccess", new { returnUrl });
                    }
                    else
                    {
                        foreach (var error in updateResult.Errors)
                        {
                            _logger.LogError($"Error during update: {error.Description}");
                        }
                        return RedirectToPage("/Account/ConfirmationFailure");
                    }
                }
                else
                {
                    _logger.LogError("Email confirmation failed.");
                    return RedirectToPage("/Account/ConfirmationFailure");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception during email confirmation: {ex.Message}");
                return StatusCode(500, $"Erreur serveur lors de la confirmation de l’email. {ex.Message}");
            }
        }


    }
}

