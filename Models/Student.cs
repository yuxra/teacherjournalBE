using System.ComponentModel.DataAnnotations;

namespace TeacherJournal.Api.Models;

public class Student
{
    public int Id { get; set; }

    [Required]
    public int ClassroomId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string NISN { get; set; } = string.Empty;

    [Required]
    public int AttendanceNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relationships
    public Classroom? Classroom { get; set; }

    public ICollection<AttendanceDetail> AttendanceDetails { get; set; }
        = new List<AttendanceDetail>();
}