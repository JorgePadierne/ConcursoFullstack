using System;
using System.Collections.Generic;

namespace Classroom_Dashboard_Backend.Models;

public partial class Submission
{
    public string Id { get; set; } = null!;

    public string? CourseworkId { get; set; }

    public string? UserEmail { get; set; }

    public string? State { get; set; }

    public bool? HandedIn { get; set; }

    public bool? Late { get; set; }

    public decimal? Grade { get; set; }

    public DateTime? LastUpdate { get; set; }

    public virtual Coursework? Coursework { get; set; }

    public virtual User? UserEmailNavigation { get; set; }
}
