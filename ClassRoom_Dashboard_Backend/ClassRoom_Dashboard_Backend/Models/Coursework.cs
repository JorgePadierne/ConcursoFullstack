using System;
using System.Collections.Generic;

namespace Classroom_Dashboard_Backend.Models;

public partial class Coursework
{
    public string Id { get; set; } = null!;

    public string? CourseId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateTime? DueDate { get; set; }

    public decimal? MaxPoints { get; set; }

    public DateTime? LastSync { get; set; }

    public virtual Course? Course { get; set; }

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
