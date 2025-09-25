using Classroom_Dashboard_Backend.Models;
using Classroom_Dashboard_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Classroom_Dashboard_Backend.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ClassroomDBContext _db;
        private readonly GoogleClassroomService _gclass;

        public DashboardController(ClassroomDBContext db, GoogleClassroomService gclass)
        {
            _db = db;
            _gclass = gclass;
        }

        private string? CurrentEmail() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("sub");

        [HttpGet("alumno")]
        public async Task<IActionResult> AlumnoSummary()
        {
            var email = CurrentEmail();
            if (email == null) return Unauthorized();

            // Very basic summary using cached submissions if exist.
            var total = await _db.Submissions.CountAsync(s => s.UserEmail == email);
            var handed = await _db.Submissions.CountAsync(s => s.UserEmail == email && s.HandedIn == true);
            var late = await _db.Submissions.CountAsync(s => s.UserEmail == email && s.Late == true);
            var pending = total - handed;
            var percent = total > 0 ? (double)handed / total * 100 : 0;

            return Ok(new
            {
                email,
                totalSubmissions = total,
                handedIn = handed,
                pending,
                late,
                completionPercent = Math.Round(percent, 2)
            });
        }

        [HttpGet("profesor")]
        public async Task<IActionResult> ProfesorSummary()
        {
            var email = CurrentEmail();
            if (email == null) return Unauthorized();

            // Aggregate progress by course using cached submissions
            var byCourse = await _db.Courseworks
                .Include(cw => cw.Submissions)
                .GroupBy(cw => cw.CourseId!)
                .Select(g => new
                {
                    courseId = g.Key,
                    tasks = g.Count(),
                    submissions = g.SelectMany(x => x.Submissions).Count(),
                    handedIn = g.SelectMany(x => x.Submissions).Count(s => s.HandedIn == true)
                })
                .ToListAsync();

            return Ok(byCourse);
        }
    }
}
