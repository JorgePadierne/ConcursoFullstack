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
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string error)
        {
            try 
            {
                Console.WriteLine($"OAuth callback received. Code: {!string.IsNullOrEmpty(code)}, Error: {error}");
                
                if (!string.IsNullOrEmpty(error))
                {
                    return BadRequest($"OAuth error: {error}");
                }
                
                if (string.IsNullOrEmpty(code))
                {
                    return BadRequest("Authorization code is missing from Google OAuth response");
                }

                // Paso 3a: Intercambiar code por tokens
                Console.WriteLine("Exchanging authorization code for tokens...");
                var tokens = await ExchangeCodeAsync(code);
                
                if (tokens == null || string.IsNullOrEmpty(tokens.AccessToken))
                {
                    return StatusCode(500, "Failed to obtain access token from Google");
                }

                Console.WriteLine("Successfully obtained access token. Getting user info...");
                
                // Paso 3b: Obtener info del usuario
                var userInfo = await GetUserInfoAsync(tokens.AccessToken);
                
                if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
                {
                    return StatusCode(500, "Failed to retrieve user information from Google");
                }
                
                Console.WriteLine($"User info retrieved. Email: {userInfo.Email}, Name: {userInfo.Name}");

                // Paso 3c: Guardar/actualizar usuario en DB
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == userInfo.Email);
                
                if (user == null)
                {
                    Console.WriteLine("Creating new user...");
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
                    Console.WriteLine("Updating existing user...");
                    user.GoogleAccessToken = tokens.AccessToken;
                    if (!string.IsNullOrEmpty(tokens.RefreshToken))
                    {
                        user.GoogleRefreshToken = _protector.Protect(tokens.RefreshToken);
                    }
                    user.TokenExpiry = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn);
                }

                await _db.SaveChangesAsync();
                Console.WriteLine("User saved to database successfully");

                // Paso 3d: Generar JWT para frontend
                var jwt = GenerateJwt(user);

                // Crear DTO para evitar problemas de serialización
                var userDto = new
                {
                    id = user.Id,
                    email = user.Email,
                    name = user.Name,
                    role = user.Role,
                    // No enviar el refresh token al frontend por seguridad
                    tokenExpiry = user.TokenExpiry
                };

                // En un entorno de producción, asegúrate de que el frontend maneje el token de manera segura
                Console.WriteLine("Authentication successful. Generating response...");
                
                // Redirigir al frontend con los tokens como parámetros de consulta
                var frontendUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:3000";
                var redirectUrl = $"{frontendUrl}/auth/callback?token={Uri.EscapeDataString(jwt)}&email={Uri.EscapeDataString(user.Email)}&name={Uri.EscapeDataString(user.Name ?? "")}&role={Uri.EscapeDataString(user.Role)}";
                
                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OAuth callback: {ex}");
                return StatusCode(500, $"An error occurred during authentication: {ex.Message}");
            }
        }

        // Paso 4: Intercambiar code por token
        private async Task<TokenResponse> ExchangeCodeAsync(string code)
        {
            var clientId = _config["Google:ClientId"];
            var clientSecret = _config["Google:ClientSecret"];
            var redirectUri = _config["Google:RedirectUri"];

            // Log the values being used (except sensitive ones in production)
            Console.WriteLine($"[ExchangeCodeAsync] Starting token exchange...");
            Console.WriteLine($"[ExchangeCodeAsync] Redirect URI: {redirectUri}");
            Console.WriteLine($"[ExchangeCodeAsync] Client ID: {clientId}");
            Console.WriteLine($"[ExchangeCodeAsync] Code received: {!string.IsNullOrEmpty(code)}");
            Console.WriteLine($"[ExchangeCodeAsync] Code length: {code?.Length ?? 0} characters");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(redirectUri))
            {
                var errorMsg = "Missing required Google OAuth configuration. Check if all required settings are in appsettings.json";
                Console.WriteLine($"[ExchangeCodeAsync] {errorMsg}");
                throw new Exception(errorMsg);
            }

            using var http = new HttpClient();
            var body = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            };

            // Log the request body (without sensitive data)
            var loggableBody = new Dictionary<string, string>(body)
            {
                ["client_secret"] = "[REDACTED]",
                ["code"] = $"{code?.Substring(0, Math.Min(10, code?.Length ?? 0))}..."
            };
            Console.WriteLine($"[ExchangeCodeAsync] Request body: {System.Text.Json.JsonSerializer.Serialize(loggableBody)}");

            try
            {
                var content = new FormUrlEncodedContent(body);
                var tokenUrl = "https://oauth2.googleapis.com/token";
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = content
                };

                Console.WriteLine($"[ExchangeCodeAsync] Sending POST request to: {tokenUrl}");
                
                // Add a timeout to prevent hanging
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await http.SendAsync(requestMessage, cts.Token);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[ExchangeCodeAsync] Google response status: {(int)response.StatusCode} {response.StatusCode}");
                Console.WriteLine($"[ExchangeCodeAsync] Google response headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value.ToArray())}"))}");
                Console.WriteLine($"[ExchangeCodeAsync] Google response content: {responseContent}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = $"Failed to exchange code for token. Status: {(int)response.StatusCode} {response.StatusCode}";
                    Console.WriteLine($"[ExchangeCodeAsync] {errorMessage}");
                    
                    // Try to parse the error response for more details
                    try
                    {
                        var errorResponse = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                        if (errorResponse != null && errorResponse.ContainsKey("error"))
                        {
                            errorMessage += $"\nError: {errorResponse["error"]}";
                            if (errorResponse.ContainsKey("error_description"))
                            {
                                errorMessage += $"\nDescription: {errorResponse["error_description"]}";
                            }
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        Console.WriteLine($"[ExchangeCodeAsync] Error parsing error response: {jsonEx}");
                    }
                    
                    throw new HttpRequestException(errorMessage);
                }
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, options);
                
                if (tokenResponse == null)
                {
                    var errorMsg = "Failed to deserialize token response: response is null";
                    Console.WriteLine($"[ExchangeCodeAsync] {errorMsg}");
                    throw new Exception(errorMsg);
                }
                
                if (string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    var errorMsg = "Access token is missing from the response";
                    Console.WriteLine($"[ExchangeCodeAsync] {errorMsg}");
                    throw new Exception(errorMsg);
                }
                
                Console.WriteLine($"[ExchangeCodeAsync] Successfully obtained access token. Expires in: {tokenResponse.ExpiresIn} seconds");
                return tokenResponse;
            }
            catch (OperationCanceledException ex) when (ex is TaskCanceledException tce && tce.InnerException is TimeoutException)
            {
                var errorMsg = "Request to Google OAuth server timed out";
                Console.WriteLine($"[ExchangeCodeAsync] {errorMsg}");
                throw new TimeoutException(errorMsg, ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExchangeCodeAsync] Error exchanging code for token: {ex}");
                throw new Exception($"Error exchanging authorization code for token: {ex.Message}", ex);
            }
        }

        // Paso 5: Obtener info básica del usuario
        private async Task<UserInfo> GetUserInfoAsync(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentException("Access token cannot be null or empty", nameof(accessToken));
            }

            Console.WriteLine($"[GetUserInfoAsync] Getting user info with access token: {accessToken.Substring(0, Math.Min(10, accessToken.Length))}...");
            
            using var http = new HttpClient();
            try
            {
                var userInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";
                var request = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                
                // Add a timeout to prevent hanging
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await http.SendAsync(request, cts.Token);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GetUserInfoAsync] User info response status: {(int)response.StatusCode} {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = $"Failed to get user info. Status: {(int)response.StatusCode} {response.StatusCode}";
                    Console.WriteLine($"[GetUserInfoAsync] {errorMessage}");
                    Console.WriteLine($"[GetUserInfoAsync] Response content: {responseContent}");
                    
                    // Try to parse the error response for more details
                    try
                    {
                        var errorResponse = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                        if (errorResponse != null && errorResponse.ContainsKey("error"))
                        {
                            errorMessage += $"\nError: {errorResponse["error"]}";
                            if (errorResponse.ContainsKey("error_description"))
                            {
                                errorMessage += $"\nDescription: {errorResponse["error_description"]}";
                            }
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        Console.WriteLine($"[GetUserInfoAsync] Error parsing error response: {jsonEx}");
                    }
                    
                    throw new HttpRequestException(errorMessage);
                }
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var userInfo = JsonSerializer.Deserialize<UserInfo>(responseContent, options);
                
                if (userInfo == null)
                {
                    throw new Exception("Failed to deserialize user info response");
                }
                
                Console.WriteLine($"[GetUserInfoAsync] Successfully retrieved user info for: {userInfo.Email}");
                return userInfo;
            }
            catch (OperationCanceledException ex) when (ex is TaskCanceledException tce && tce.InnerException is TimeoutException)
            {
                var errorMsg = "Request to Google UserInfo endpoint timed out";
                Console.WriteLine($"[GetUserInfoAsync] {errorMsg}");
                throw new TimeoutException(errorMsg, ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetUserInfoAsync] Error getting user info: {ex}");
                throw new Exception($"Error getting user information: {ex.Message}", ex);
            }
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
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }

    public class UserInfo
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
