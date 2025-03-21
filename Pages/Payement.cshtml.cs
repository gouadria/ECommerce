using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Threading.Tasks;

namespace ECommerce.Pages
{

   
    public class PayPalPageModel : PageModel
    {
        private readonly PayPalService _payPalService;
        private readonly UserManager<IdentityUser> _userManager;

        [BindProperty]
        public string UserEmail { get; set; }
        public string PhoneNumber { get; set; }

        public decimal Amount { get; set; }
        public string ApprovalUrl { get; set; }
        public string ErrorMessage { get; set; }

        public PayPalPageModel(PayPalService payPalService, UserManager<IdentityUser> userManager)
        {
            _payPalService = payPalService;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync(decimal total)
        {
            Amount = total;

            // Récupérer l'email et le téléphone de l'utilisateur connecté
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                UserEmail = user.Email;
                PhoneNumber = user.PhoneNumber; // Assurez-vous que le champ PhoneNumber existe
            }

            return Page();
        }



        public async Task<IActionResult> OnPostAsync()
        {
            if (!decimal.TryParse(Request.Form["Amount"], out decimal amount))
            {
                TempData["ErrorMessage"] = "Montant invalide.";
                return Page();
            }

            Amount = amount;
            var paymentMode = Request.Form["paymentMode"];

            // Récupérer l'utilisateur connecté
            var user = await _userManager.GetUserAsync(User);
            string email = user?.Email ?? "email_defaut@example.com";
            string phone = user?.PhoneNumber ?? "0000000000"; // Par défaut si le numéro est absent

            if (paymentMode == "paypal")
            {
                var result = await _payPalService.CreatePayPalOrderAsync(Amount, email, phone);

                if (result.StartsWith("Erreur"))
                {
                    TempData["ErrorMessage"] = result;
                    return Page();
                }

                return Redirect(result);
            }

            return Page();
        }







    }
}

