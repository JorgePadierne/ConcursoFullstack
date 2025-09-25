using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Classroom_Dashboard_Backend.Models;

public partial class ClassroomDBContext : DbContext
{
    public ClassroomDBContext()
    {
    }

    public ClassroomDBContext(DbContextOptions<ClassroomDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Coursework> Courseworks { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Submission> Submissions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Connection is configured via DI in Program.cs
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("attendance_pkey");

            entity.ToTable("attendance");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Present).HasColumnName("present");
            entity.Property(e => e.UserEmail).HasColumnName("user_email");

            entity.HasOne(d => d.Course).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("attendance_course_id_fkey");

            entity.HasOne(d => d.UserEmailNavigation).WithMany(p => p.Attendances)
                .HasPrincipalKey(p => p.Email)
                .HasForeignKey(d => d.UserEmail)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("attendance_user_email_fkey");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("courses_pkey");

            entity.ToTable("courses");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CourseState).HasColumnName("course_state");
            entity.Property(e => e.LastSync)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_sync");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Section).HasColumnName("section");
            entity.Property(e => e.TeacherEmails).HasColumnName("teacher_emails");
        });

        modelBuilder.Entity<Coursework>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("coursework_pkey");

            entity.ToTable("coursework");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DueDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("due_date");
            entity.Property(e => e.LastSync)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_sync");
            entity.Property(e => e.MaxPoints).HasColumnName("max_points");
            entity.Property(e => e.Title).HasColumnName("title");

            entity.HasOne(d => d.Course).WithMany(p => p.Courseworks)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("coursework_course_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Link).HasColumnName("link");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Read)
                .HasDefaultValue(false)
                .HasColumnName("read");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("sent_at");
            entity.Property(e => e.UserEmail).HasColumnName("user_email");

            entity.HasOne(d => d.UserEmailNavigation).WithMany(p => p.Notifications)
                .HasPrincipalKey(p => p.Email)
                .HasForeignKey(d => d.UserEmail)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("notifications_user_email_fkey");
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("submissions_pkey");

            entity.ToTable("submissions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CourseworkId).HasColumnName("coursework_id");
            entity.Property(e => e.Grade).HasColumnName("grade");
            entity.Property(e => e.HandedIn).HasColumnName("handed_in");
            entity.Property(e => e.LastUpdate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_update");
            entity.Property(e => e.Late).HasColumnName("late");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.UserEmail).HasColumnName("user_email");

            entity.HasOne(d => d.Coursework).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.CourseworkId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("submissions_coursework_id_fkey");

            entity.HasOne(d => d.UserEmailNavigation).WithMany(p => p.Submissions)
                .HasPrincipalKey(p => p.Email)
                .HasForeignKey(d => d.UserEmail)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("submissions_user_email_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.GoogleAccessToken).HasColumnName("google_access_token");
            entity.Property(e => e.GoogleRefreshToken).HasColumnName("google_refresh_token");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Role)
                .HasDefaultValueSql("'student'::text")
                .HasColumnName("role");
            entity.Property(e => e.TokenExpiry)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("token_expiry");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
