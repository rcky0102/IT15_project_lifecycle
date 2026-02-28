using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace project_lifecycle.ExecutiveArea.Controllers
{
    [Area("Executive")]
    [Authorize(Roles = "Executive")]
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}