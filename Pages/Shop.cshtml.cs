using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Pages
{
    [AllowAnonymous]
    public class ShopModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
