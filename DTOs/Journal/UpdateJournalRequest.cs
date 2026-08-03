using System.ComponentModel.DataAnnotations;

namespace TeacherJournal.Api.DTOs.Journal;

public class UpdateJournalRequest
{
    [Required]
    public DateTime Date { get; set; }

    [Required]
    public int ClassroomId { get; set; }

    [Required]
    public int SubjectId { get; set; }

    // Optional
    [MaxLength(100)]
    public string? LessonPeriod { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Competency { get; set; } = string.Empty;

    // Optional
    [MaxLength(2000)]
    public string? Material { get; set; }

    [Required]
    public int AttendanceId { get; set; }
}