using Classroom_Dashboard_Backend.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Classroom.v1;
using Google.Apis.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Classroom_Dashboard_Backend.Services
{
    public class GoogleClassroomService
    {
        private readonly ClassroomDBContext _db;
        private readonly IConfiguration _config;
        private readonly IDataProtector _protector;

        public GoogleClassroomService(ClassroomDBContext db, IConfiguration config, IDataProtectionProvider dataProtectionProvider)
        {
            _db = db;
            _config = config;
            _protector = dataProtectionProvider.CreateProtector("GoogleRefreshToken");
        }

        public async Task<ClassroomService?> GetServiceForUserAsync(User user)
        {
            var accessToken = user.GoogleAccessToken;

            // Refresh token if expired
            if (!user.TokenExpiry.HasValue || user.TokenExpiry.Value <= DateTime.UtcNow.AddMinutes(1))
            {
                if (!string.IsNullOrEmpty(user.GoogleRefreshToken))
                {
                    var refreshed = await RefreshTokenAsync(_protector.Unprotect(user.GoogleRefreshToken));
                    if (refreshed != null)
                    {
                        user.GoogleAccessToken = refreshed.AccessToken;
                        if (!string.IsNullOrEmpty(refreshed.RefreshToken))
                        {
                            user.GoogleRefreshToken = _protector.Protect(refreshed.RefreshToken);
                        }
                        user.TokenExpiry = DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn);
                        await _db.SaveChangesAsync();
                        accessToken = refreshed.AccessToken;
                    }
                }
            }

            if (string.IsNullOrEmpty(accessToken)) return null;

            var credential = GoogleCredential.FromAccessToken(accessToken);
            var service = new ClassroomService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "ClassroomDashboard"
            });
            return service;
        }

        public async Task<IList<Google.Apis.Classroom.v1.Data.Course>> GetCoursesAsync(User user)
        {
            var service = await GetServiceForUserAsync(user);
            if (service == null) return new List<Google.Apis.Classroom.v1.Data.Course>();
            var req = service.Courses.List();
            req.CourseStates = CoursesResource.ListRequest.CourseStatesEnum.ACTIVE;
            var result = await req.ExecuteAsync();
            return result.Courses ?? new List<Google.Apis.Classroom.v1.Data.Course>();
        }

        public async Task<IList<Google.Apis.Classroom.v1.Data.CourseWork>> GetCourseworkAsync(User user, string courseId)
        {
            var service = await GetServiceForUserAsync(user);
            if (service == null) return new List<Google.Apis.Classroom.v1.Data.CourseWork>();
            var req = service.Courses.CourseWork.List(courseId);
            var result = await req.ExecuteAsync();
            return result.CourseWork ?? new List<Google.Apis.Classroom.v1.Data.CourseWork>();
        }

        private async Task<TokenRefreshResponse?> RefreshTokenAsync(string refreshToken)
        {
            using var http = new HttpClient();
            var body = new Dictionary<string, string>
            {
                {"client_id", _config["Google:ClientId"]!},
                {"client_secret", _config["Google:ClientSecret"]!},
                {"refresh_token", refreshToken},
                {"grant_type", "refresh_token"}
            };
            var resp = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(body));
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TokenRefreshResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }

    public class TokenRefreshResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = string.Empty;
    }
}
