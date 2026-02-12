using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace project_lifecycle.EmployeeArea.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")] 
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
