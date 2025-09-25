using Classroom_Dashboard_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Classroom_Dashboard_Backend.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ClassroomDBContext _db;
        public NotificationsController(ClassroomDBContext db)
        {
            _db = db;
        }

        public class SendNotificationRequest
        {
            public string? UserEmail { get; set; }
            public string? Message { get; set; }
            public string? Link { get; set; }
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendNotificationRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Message)) return BadRequest("Message is required");

            // If not coordinator/teacher, can only send to self
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "student";
            var senderEmail = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("sub");
            var targetEmail = string.IsNullOrWhiteSpace(req.UserEmail) ? senderEmail : req.UserEmail;

            if (role == "student" && targetEmail != senderEmail)
                return Forbid();

            var exists = await _db.Users.AnyAsync(u => u.Email == targetEmail);
            if (!exists) return NotFound("User not found");

            var notif = new Notification
            {
                UserEmail = targetEmail,
                Message = req.Message,
                Link = req.Link,
                SentAt = DateTime.UtcNow,
                Read = false
            };
            _db.Notifications.Add(notif);
            await _db.SaveChangesAsync();

            return Ok(new { id = notif.Id, userEmail = notif.UserEmail, message = notif.Message, link = notif.Link });
        }
    }
}
