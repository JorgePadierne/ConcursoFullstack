using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Classroom_Dashboard_Backend.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        private readonly IConfiguration _config;
        
        public TestController(IConfiguration config)
        {
            _config = config;
        }
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { 
                status = "OK", 
                timestamp = DateTime.UtcNow,
                message = "Backend funcionando correctamente" 
            });
        }

        [HttpGet("protected")]
        [Authorize]
        public IActionResult Protected()
        {
            return Ok(new { 
                message = "Este endpoint requiere JWT",
                user = User.Identity?.Name ?? "Unknown",
                claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }

        [HttpGet("generate-jwt")]
        public IActionResult GenerateTestJWT(string email = "test@example.com", string role = "student")
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(ClaimTypes.NameIdentifier, email),
                new Claim(ClaimTypes.Email, email),
                new Claim("name", "Test User"),
                new Claim("role", role),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            
            return Ok(new { 
                token = jwt,
                email = email,
                role = role,
                expires = DateTime.UtcNow.AddHours(2)
            });
        }
    }
}
