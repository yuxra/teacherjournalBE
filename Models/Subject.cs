using System.ComponentModel.DataAnnotations;

namespace TeacherJournal.Api.Models;

public class Subject
{
    public int Id { get; set; }

    [Required]
    public int TeacherId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relationships
    public Teacher? Teacher { get; set; }

    public ICollection<SubjectClass> SubjectClasses { get; set; }
        = new List<SubjectClass>();

    public ICollection<Attendance> Attendances { get; set; }
        = new List<Attendance>();

    public ICollection<Journal> Journals { get; set; }
        = new List<Journal>();
}