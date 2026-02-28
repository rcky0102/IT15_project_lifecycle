using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace project_lifecycle.Areas.ProjectManager.Controllers
{
    [Area("ProjectManager")]
    [Authorize(Roles = "ProjectManager")]
    public class MessagesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
