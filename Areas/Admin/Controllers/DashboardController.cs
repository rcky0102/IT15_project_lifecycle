using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using project_lifecycle.Constants;

namespace project_lifecycle.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
