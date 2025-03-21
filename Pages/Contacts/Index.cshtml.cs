using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ECommerce.Models;

namespace ECommerce.Pages.Contacts
{
    public class IndexModel : PageModel
    {
        private readonly ECommerce.Models.EcommerceDbContext _context;

        public IndexModel(ECommerce.Models.EcommerceDbContext context)
        {
            _context = context;
        }

        public IList<Contact> Contact { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Contact = await _context.Contacts.ToListAsync();
        }
    }
}
