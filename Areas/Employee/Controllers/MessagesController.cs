using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace project_lifecycle.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class MessagesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
