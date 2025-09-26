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
        public async Task<IActionResult> Get()
        {
            var email = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound();

            // Crear DTO para evitar problemas de serialización
            var userDto = new
            {
                id = user.Id,
                email = user.Email,
                name = user.Name,
                role = user.Role,
                googleRefreshToken = user.GoogleRefreshToken,
                googleAccessToken = user.GoogleAccessToken,
                tokenExpiry = user.TokenExpiry
            };

            return Ok(userDto);
        }
    }
}
