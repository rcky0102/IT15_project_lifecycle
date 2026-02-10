using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using project_lifecycle.Constants;

namespace project_lifecycle.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
