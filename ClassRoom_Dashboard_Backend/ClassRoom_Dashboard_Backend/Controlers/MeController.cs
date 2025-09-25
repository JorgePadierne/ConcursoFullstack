using Classroom_Dashboard_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Classroom_Dashboard_Backend.Controllers
{
    [ApiController]
    [Route("api/me")]
    [Authorize]
    public class MeController : ControllerBase
    {
        private readonly ClassroomDBContext _db;
        public MeController(ClassroomDBContext db) { _db = db; }

        [HttpGet]
        public IActionResult Get()
        {
            var email = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("sub");
            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? "student";
            return Ok(new { email, role });
        }
    }
}
