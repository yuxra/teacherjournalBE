using System.ComponentModel.DataAnnotations;
using TeacherJournal.Api.Models;

namespace TeacherJournal.Api.DTOs.Attendance;

public class CreateAttendanceRequest
{
    [Required]
    public int ClassroomId { get; set; }

    [Required]
    public int SubjectId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [MinLength(1)]
    public List<AttendanceStudentRequest> Students { get; set; }
        = new();
}

public class AttendanceStudentRequest
{
    [Required]
    public int StudentId { get; set; }

    [Required]
    public AttendanceStatus Status { get; set; }
}