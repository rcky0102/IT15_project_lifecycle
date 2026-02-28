using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace project_lifecycle.Areas.Executive.Controllers
{
    [Area("Executive")]
    [Authorize(Roles = "Executive")]
    public class MessagesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
