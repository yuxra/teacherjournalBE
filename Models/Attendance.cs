using System.ComponentModel.DataAnnotations;

namespace TeacherJournal.Api.Models;

public class Attendance
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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relationships
    public Teacher? Teacher { get; set; }

    public Classroom? Classroom { get; set; }

    public Subject? Subject { get; set; }

    public ICollection<AttendanceDetail> Details { get; set; }
        = new List<AttendanceDetail>();

    public ICollection<Journal> Journals { get; set; }
        = new List<Journal>();
}