using System.ComponentModel.DataAnnotations;

namespace TeacherJournal.Api.Models;

public class Teacher
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relationships
    public ICollection<Classroom> Classrooms { get; set; } = new List<Classroom>();

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();

    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public ICollection<Journal> Journals { get; set; } = new List<Journal>();
}