using System.ComponentModel.DataAnnotations;
using TeacherJournal.Api.Models;

namespace TeacherJournal.Api.DTOs.Attendance;

public class UpdateAttendanceRequest
{
    [Required]
    public DateTime Date { get; set; }

    [Required]
    [MinLength(1)]
    public List<AttendanceStudentRequest> Students { get; set; }
        = new();
}