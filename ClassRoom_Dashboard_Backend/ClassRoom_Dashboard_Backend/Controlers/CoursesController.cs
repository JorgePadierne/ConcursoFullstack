using Classroom_Dashboard_Backend.Models;
using Classroom_Dashboard_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Classroom_Dashboard_Backend.Controllers
{
    [ApiController]
    [Route("api/courses")]
    [Authorize]
    public class CoursesController : ControllerBase
    {
        private readonly ClassroomDBContext _db;
        private readonly GoogleClassroomService _gclass;

        public CoursesController(ClassroomDBContext db, GoogleClassroomService gclass)
        {
            _db = db;
            _gclass = gclass;
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var email = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(email)) return null;
            return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        [HttpGet]
        public async Task<IActionResult> GetCourses()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            // Fetch from Google and return (basic implementation; caching could be added)
            var courses = await _gclass.GetCoursesAsync(user);

            // Map to minimal response
            var result = courses.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                section = c.Section,
                courseState = c.CourseState
            });
            return Ok(result);
        }

        [HttpGet("{courseId}/coursework")]
        public async Task<IActionResult> GetCoursework(string courseId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var items = await _gclass.GetCourseworkAsync(user, courseId);
            var result = items.Select(w => new
            {
                id = w.Id,
                title = w.Title,
                description = w.Description,
                dueDate = w.DueDate?.ToString(),
                maxPoints = w.MaxPoints
            });
            return Ok(result);
        }
    }
}
