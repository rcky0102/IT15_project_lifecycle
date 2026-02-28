using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace project_lifecycle.Areas.HumanResource.Controllers
{
    [Area("HumanResource")]
    [Authorize(Roles = "HumanResource")]
    public class MessagesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
