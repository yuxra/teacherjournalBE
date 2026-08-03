using System.ComponentModel.DataAnnotations;

namespace TeacherJournal.Api.DTOs.Subject;

public class CreateSubjectRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
}