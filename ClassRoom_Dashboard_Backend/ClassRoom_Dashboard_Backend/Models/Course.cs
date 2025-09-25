using System;
using System.Collections.Generic;

namespace Classroom_Dashboard_Backend.Models;

public partial class Course
{
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    public string? Section { get; set; }

    public string? CourseState { get; set; }

    public List<string>? TeacherEmails { get; set; }

    public DateTime? LastSync { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Coursework> Courseworks { get; set; } = new List<Coursework>();
}
