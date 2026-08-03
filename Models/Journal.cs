using System.ComponentModel.DataAnnotations;

namespace TeacherJournal.Api.Models;

public class Journal
{
    public int Id { get; set; }

    [Required]
    public int TeacherId { get; set; }

    [Required]
    public int ClassroomId { get; set; }

    [Required]
    public int SubjectId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    // Optional
    public string? LessonPeriod { get; set; }

    [Required]
    public string CompetencyStandard { get; set; } = string.Empty;

    // Optional
    public string? Material { get; set; }

    [Required]
    public int AttendanceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relationships
    public Teacher? Teacher { get; set; }

    public Classroom? Classroom { get; set; }

    public Subject? Subject { get; set; }

    public Attendance? Attendance { get; set; }
}