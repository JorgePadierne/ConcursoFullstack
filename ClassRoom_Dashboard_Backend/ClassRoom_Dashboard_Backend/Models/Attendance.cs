using System;
using System.Collections.Generic;

namespace Classroom_Dashboard_Backend.Models;

public partial class Attendance
{
    public int Id { get; set; }

    public string? CourseId { get; set; }

    public DateOnly? Date { get; set; }

    public string? UserEmail { get; set; }

    public bool? Present { get; set; }

    public virtual Course? Course { get; set; }

    public virtual User? UserEmailNavigation { get; set; }
}
