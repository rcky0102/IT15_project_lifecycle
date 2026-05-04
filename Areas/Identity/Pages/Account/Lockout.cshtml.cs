using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace project_lifecycle.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LockoutModel : PageModel
    {
        private readonly project_lifecycle.Data.ApplicationDbContext _context;

        public LockoutModel(project_lifecycle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public string SuperAdminContact { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var admin = await _context.SuperAdmins.FirstOrDefaultAsync();
            if (admin != null)
            {
                SuperAdminContact = admin.Contact ?? string.Empty;
            }
        }
    }
}
