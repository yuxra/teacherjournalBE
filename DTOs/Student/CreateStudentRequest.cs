using System.ComponentModel.DataAnnotations;

namespace TeacherJournal.Api.DTOs.Student;

public class CreateStudentRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string NISN { get; set; } = string.Empty;

    [Required]
    [Range(1, 999)]
    public int AttendanceNumber { get; set; }
}