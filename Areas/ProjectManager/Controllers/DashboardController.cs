using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace project_lifecycle.ProjectManagerArea.Controllers
{
    [Area("ProjectManager")]
    [Authorize(Roles = "ProjectManager")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
