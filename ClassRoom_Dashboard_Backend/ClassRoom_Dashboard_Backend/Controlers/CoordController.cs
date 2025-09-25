using Classroom_Dashboard_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Classroom_Dashboard_Backend.Controllers
{
    [ApiController]
    [Route("api/coord")]
    [Authorize(Roles = "coordinator,admin")] // Adjust roles as you need
    public class CoordController : ControllerBase
    {
        private readonly ClassroomDBContext _db;
        public CoordController(ClassroomDBContext db)
        {
            _db = db;
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> Metrics()
        {
            var users = await _db.Users.CountAsync();
            var students = await _db.Users.CountAsync(u => u.Role == "student");
            var teachers = await _db.Users.CountAsync(u => u.Role == "teacher");
            var courses = await _db.Courses.CountAsync();
            var coursework = await _db.Courseworks.CountAsync();
            var submissions = await _db.Submissions.CountAsync();
            var handedIn = await _db.Submissions.CountAsync(s => s.HandedIn == true);
            var late = await _db.Submissions.CountAsync(s => s.Late == true);

            return Ok(new
            {
                users,
                students,
                teachers,
                courses,
                coursework,
                submissions,
                handedIn,
                late
            });
        }
    }
}
