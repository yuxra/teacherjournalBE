using System.ComponentModel.DataAnnotations;

namespace TeacherJournal.Api.DTOs.Recap;

public class AttendanceRecapQuery
{
    [Range(2000, 2100)]
    public int Year { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }

    [Required]
    public int ClassroomId { get; set; }

    [Required]
    public int SubjectId { get; set; }
}