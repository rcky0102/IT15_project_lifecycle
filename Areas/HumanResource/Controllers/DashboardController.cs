using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace project_lifecycle.HumanResourceArea.Controllers
{
    [Area("HumanResource")]
    [Authorize(Roles = "HumanResource")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
