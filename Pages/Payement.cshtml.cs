using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace ECommerce.Pages
{
    public class PayPalPageModel : PageModel
    {
        private readonly PayPalService _payPalService;
        private readonly UserManager<IdentityUser> _userManager;

        [BindProperty]
        public string? UserEmail { get; set; }  // Nullable
        public string? PhoneNumber { get; set; }  // Nullable

        public decimal Amount { get; set; }
        public string? ApprovalUrl { get; set; }  // Nullable
        public string? ErrorMessage { get; set; }  // Nullable

        // Constructeur
        public PayPalPageModel(PayPalService payPalService, UserManager<IdentityUser> userManager)
        {
            _payPalService = payPalService;
            _userManager = userManager;

            // Initialiser les propriétés pour éviter les erreurs de non-nullabilité
            UserEmail = string.Empty;  // Valeur par défaut
            PhoneNumber = string.Empty;  // Valeur par défaut
            ApprovalUrl = string.Empty;  // Valeur par défaut
            ErrorMessage = string.Empty;  // Valeur par défaut
        }

        // Méthode pour récupérer l'utilisateur et le montant
        public async Task<IActionResult> OnGetAsync(decimal total)
        {
            Amount = total;

            // Récupérer l'email et le téléphone de l'utilisateur connecté
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                UserEmail = user.Email ?? "default@example.com";  // Utiliser un email par défaut si null
                PhoneNumber = user.PhoneNumber ?? "0000000000";  // Utiliser un numéro par défaut si null
            }
            else
            {
                // Si l'utilisateur est introuvable, on initialise avec des valeurs par défaut
                UserEmail = "default@example.com";
                PhoneNumber = "0000000000";
            }

            return Page();
        }

        // Méthode pour traiter la demande de paiement
        public async Task<IActionResult> OnPostAsync()
        {
            // Validation du montant
            if (!decimal.TryParse(Request.Form["Amount"], out decimal amount))
            {
                TempData["ErrorMessage"] = "Montant invalide.";
                return Page();
            }

            Amount = amount;
            var paymentMode = Request.Form["paymentMode"];

            // Récupérer l'utilisateur connecté
            var user = await _userManager.GetUserAsync(User);
            string email = user?.Email ?? "email_defaut@example.com";  // Email par défaut si l'utilisateur est null
            string phone = user?.PhoneNumber ?? "0000000000";  // Numéro de téléphone par défaut si l'utilisateur est null

            if (paymentMode == "paypal")
            {
                // Création de la commande PayPal
                var result = await _payPalService.CreatePayPalOrderAsync(Amount, email, phone);

                if (result.StartsWith("Erreur"))
                {
                    // Si une erreur survient dans la création de la commande, on l'affiche dans la page
                    TempData["ErrorMessage"] = result;
                    return Page();
                }

                // Si la commande PayPal est créée avec succès, on redirige vers l'URL d'approbation
                return Redirect(result);
            }

            return Page();
        }
    }
}
