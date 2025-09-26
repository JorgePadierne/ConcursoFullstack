using Classroom_Dashboard_Backend.Models; 
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Classroom_Dashboard_Backend.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ClassroomDBContext _db;
        private readonly IDataProtector _protector;

        public AuthController(IConfiguration config, ClassroomDBContext db, IDataProtectionProvider provider)
        {
            _config = config;
            _db = db;
            _protector = provider.CreateProtector("GoogleRefreshToken");
        }

        // Paso 2: Endpoint para iniciar login
        [AllowAnonymous]
        [HttpGet("google")]
        public IActionResult GoogleLogin()
        {
            var clientId = _config["Google:ClientId"];
            var redirectUri = _config["Google:RedirectUri"];
            var scope = HttpUtility.UrlEncode(
                "openid email profile " +
                "https://www.googleapis.com/auth/classroom.courses.readonly " +
                "https://www.googleapis.com/auth/classroom.rosters.readonly " +
                "https://www.googleapis.com/auth/classroom.coursework.me " +
                "https://www.googleapis.com/auth/classroom.coursework.students " +
                "https://www.googleapis.com/auth/classroom.student-submissions.students.readonly"
            );

            var url = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                      $"client_id={clientId}&response_type=code&scope={scope}" +
                      $"&redirect_uri={redirectUri}&access_type=offline&prompt=consent";

            return Redirect(url);
        }

        // Paso 3: Callback de Google
        [AllowAnonymous]
        [HttpGet("oauth2/callback")]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest("Code missing from Google OAuth");

            // Paso 3a: Intercambiar code por tokens
            var tokens = await ExchangeCodeAsync(code);

            // Paso 3b: Obtener info del usuario
            var userInfo = await GetUserInfoAsync(tokens.AccessToken);

            // Paso 3c: Guardar/actualizar usuario en DB
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == userInfo.Email);
            if (user == null)
            {
                user = new User
                {
                    Email = userInfo.Email,
                    Name = userInfo.Name,
                    Role = "student",
                    GoogleAccessToken = tokens.AccessToken,
                    GoogleRefreshToken = string.IsNullOrEmpty(tokens.RefreshToken) ? null : _protector.Protect(tokens.RefreshToken),
                    TokenExpiry = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn)
                };
                _db.Users.Add(user);
            }
            else
            {
                user.GoogleAccessToken = tokens.AccessToken;
                if (!string.IsNullOrEmpty(tokens.RefreshToken))
                {
                    user.GoogleRefreshToken = _protector.Protect(tokens.RefreshToken);
                }
                user.TokenExpiry = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn);
            }

            await _db.SaveChangesAsync();

            // Paso 3d: Generar JWT para frontend
            var jwt = GenerateJwt(user);

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

            return Ok(new { token = jwt, user = userDto, expiresIn = 7200 });
        }
        }

        // Paso 4: Intercambiar code por token
        private async Task<TokenResponse> ExchangeCodeAsync(string code)
        {
            var clientId = _config["Google:ClientId"];
            var clientSecret = _config["Google:ClientSecret"];
            var redirectUri = _config["Google:RedirectUri"];

            using var http = new HttpClient();
            var body = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            };

            var resp = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(body));
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // Paso 5: Obtener info básica del usuario
        private async Task<UserInfo> GetUserInfoAsync(string accessToken)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var resp = await http.GetStringAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            return JsonSerializer.Deserialize<UserInfo>(resp, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // Paso 6: Generar JWT
        private string GenerateJwt(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("name", user.Name ?? string.Empty),
                new Claim("role", user.Role),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // Modelos auxiliares
    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
    }

    public class UserInfo
    {
        public string Email { get; set; }
        public string Name { get; set; }
    }
}
