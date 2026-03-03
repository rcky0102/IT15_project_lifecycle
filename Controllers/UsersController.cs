using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_lifecycle.Data;

namespace project_lifecycle.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public UsersController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Unauthorized();

            var s = q?.Trim().ToLower() ?? "";

            // Search across role tables similar to MessagesController.GetContacts
            var results = new List<object>();

            var empQuery = _db.Employees.Include(e => e.User).Where(e => e.UserId != currentUserId);
            if (!string.IsNullOrEmpty(s)) empQuery = empQuery.Where(e => e.FirstName.ToLower().Contains(s) || e.LastName.ToLower().Contains(s) || (e.User != null && e.User.Email != null && e.User.Email.ToLower().Contains(s)));
            var emps = await empQuery.Take(50).ToListAsync();
            results.AddRange(emps.Select(e => new { id = e.UserId, name = e.FirstName + " " + e.LastName, email = e.User?.Email }));

            var pmQuery = _db.ProjectManagers.Where(p => p.UserId != currentUserId);
            if (!string.IsNullOrEmpty(s)) pmQuery = pmQuery.Where(p => p.FirstName.ToLower().Contains(s) || p.LastName.ToLower().Contains(s));
            var pms = await pmQuery.Take(50).ToListAsync();
            results.AddRange(pms.Select(p => new { id = p.UserId, name = p.FirstName + " " + p.LastName, email = (string?)null }));

            var dhQuery = _db.DepartmentHeads.Where(d => d.UserId != currentUserId);
            if (!string.IsNullOrEmpty(s)) dhQuery = dhQuery.Where(d => d.FirstName.ToLower().Contains(s) || d.LastName.ToLower().Contains(s));
            var dhs = await dhQuery.Take(50).ToListAsync();
            results.AddRange(dhs.Select(d => new { id = d.UserId, name = d.FirstName + " " + d.LastName, email = (string?)null }));

            var hrQuery = _db.HumanResources.Where(h => h.UserId != currentUserId);
            if (!string.IsNullOrEmpty(s)) hrQuery = hrQuery.Where(h => h.FirstName.ToLower().Contains(s) || h.LastName.ToLower().Contains(s));
            var hrs = await hrQuery.Take(50).ToListAsync();
            results.AddRange(hrs.Select(h => new { id = h.UserId, name = h.FirstName + " " + h.LastName, email = (string?)null }));

            var execQuery = _db.Executives.Where(e => e.UserId != currentUserId);
            if (!string.IsNullOrEmpty(s)) execQuery = execQuery.Where(e => e.FirstName.ToLower().Contains(s) || e.LastName.ToLower().Contains(s));
            var execs = await execQuery.Take(50).ToListAsync();
            results.AddRange(execs.Select(e => new { id = e.UserId, name = e.FirstName + " " + e.LastName, email = (string?)null }));

            // dedupe by id
            var dedup = results.GroupBy(r => ((dynamic)r).id).Select(g => g.First()).Take(50).ToList();
            return Ok(dedup);
        }
    }
}
