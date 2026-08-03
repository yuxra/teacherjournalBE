using Microsoft.EntityFrameworkCore;
using TeacherJournal.Api.Models;

namespace TeacherJournal.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<SubjectClass> SubjectClasses => Set<SubjectClass>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<AttendanceDetail> AttendanceDetails => Set<AttendanceDetail>();
    public DbSet<Journal> Journals => Set<Journal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Teacher>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<Classroom>()
            .HasOne(x => x.Teacher)
            .WithMany(x => x.Classrooms)
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Classroom>()
            .HasIndex(x => new { x.TeacherId, x.Name })
            .IsUnique();

        modelBuilder.Entity<Student>()
            .HasOne(x => x.Classroom)
            .WithMany(x => x.Students)
            .HasForeignKey(x => x.ClassroomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Student>()
            .HasIndex(x => new { x.ClassroomId, x.AttendanceNumber })
            .IsUnique();

        modelBuilder.Entity<Student>()
            .HasIndex(x => x.NISN);

        modelBuilder.Entity<Subject>()
            .HasOne(x => x.Teacher)
            .WithMany(x => x.Subjects)
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Subject>()
            .HasIndex(x => new { x.TeacherId, x.Name })
            .IsUnique();

        modelBuilder.Entity<SubjectClass>()
            .HasOne(x => x.Subject)
            .WithMany(x => x.SubjectClasses)
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SubjectClass>()
            .HasOne(x => x.Classroom)
            .WithMany(x => x.SubjectClasses)
            .HasForeignKey(x => x.ClassroomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SubjectClass>()
            .HasIndex(x => new { x.SubjectId, x.ClassroomId })
            .IsUnique();

        modelBuilder.Entity<Attendance>()
            .HasOne(x => x.Teacher)
            .WithMany(x => x.Attendances)
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasOne(x => x.Classroom)
            .WithMany(x => x.Attendances)
            .HasForeignKey(x => x.ClassroomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasOne(x => x.Subject)
            .WithMany(x => x.Attendances)
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasIndex(x => new
            {
                x.TeacherId,
                x.ClassroomId,
                x.SubjectId,
                x.Date
            })
            .IsUnique();

        modelBuilder.Entity<AttendanceDetail>()
            .HasOne(x => x.Attendance)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.AttendanceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AttendanceDetail>()
            .HasOne(x => x.Student)
            .WithMany(x => x.AttendanceDetails)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AttendanceDetail>()
            .HasIndex(x => new
            {
                x.AttendanceId,
                x.StudentId
            })
            .IsUnique();

        modelBuilder.Entity<Journal>()
            .HasOne(x => x.Teacher)
            .WithMany(x => x.Journals)
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Journal>()
            .HasOne(x => x.Classroom)
            .WithMany(x => x.Journals)
            .HasForeignKey(x => x.ClassroomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Journal>()
            .HasOne(x => x.Subject)
            .WithMany(x => x.Journals)
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Journal>()
            .HasOne(x => x.Attendance)
            .WithMany(x => x.Journals)
            .HasForeignKey(x => x.AttendanceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Journal>()
            .Property(x => x.CompetencyStandard)
            .HasMaxLength(1000);

        modelBuilder.Entity<Journal>()
            .Property(x => x.Material)
            .HasMaxLength(2000);
    }
}