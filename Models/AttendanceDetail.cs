using System.ComponentModel.DataAnnotations;

namespace TeacherJournal.Api.Models;

public class AttendanceDetail
{
    public int Id { get; set; }

    [Required]
    public int AttendanceId { get; set; }

    [Required]
    public int StudentId { get; set; }

    [Required]
    public AttendanceStatus Status { get; set; }

    // Relationships
    public Attendance? Attendance { get; set; }

    public Student? Student { get; set; }
}