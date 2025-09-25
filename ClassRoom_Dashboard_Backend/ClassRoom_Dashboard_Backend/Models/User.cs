using System;
using System.Collections.Generic;

namespace Classroom_Dashboard_Backend.Models;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string? Name { get; set; }

    public string Role { get; set; } = null!;

    public string? GoogleRefreshToken { get; set; }

    public string? GoogleAccessToken { get; set; }

    public DateTime? TokenExpiry { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
