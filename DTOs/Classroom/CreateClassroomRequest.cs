using System.ComponentModel.DataAnnotations;

namespace TeacherJournal.Api.DTOs.Classroom;

public class CreateClassroomRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}