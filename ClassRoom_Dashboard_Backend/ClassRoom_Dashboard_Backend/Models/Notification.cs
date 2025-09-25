using System;
using System.Collections.Generic;

namespace Classroom_Dashboard_Backend.Models;

public partial class Notification
{
    public int Id { get; set; }

    public string? UserEmail { get; set; }

    public string? Message { get; set; }

    public string? Link { get; set; }

    public DateTime? SentAt { get; set; }

    public bool? Read { get; set; }

    public virtual User? UserEmailNavigation { get; set; }
}
