using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace project_lifecycle.Areas.DepartmentHead.Controllers
{
    [Area("DepartmentHead")]
    [Authorize(Roles = "DepartmentHead")]
    public class MessagesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
