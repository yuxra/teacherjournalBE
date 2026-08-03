using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherJournal.Api.Data;
using TeacherJournal.Api.DTOs.Recap;
using TeacherJournal.Api.Models;

namespace TeacherJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecapsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RecapsController(AppDbContext db)
    {
        _db = db;
    }

    // =====================================================
    // ATTENDANCE RECAP
    // =====================================================

    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendanceRecap(
        [FromQuery] AttendanceRecapQuery query)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        // -------------------------------------------------
        // VALIDATE CLASSROOM
        // -------------------------------------------------

        var classroom = await _db.Classrooms
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == query.ClassroomId &&
                x.TeacherId == teacherId);

        if (classroom == null)
        {
            return NotFound(new
            {
                message = "Kelas tidak ditemukan."
            });
        }

        // -------------------------------------------------
        // VALIDATE SUBJECT
        // -------------------------------------------------

        var subject = await _db.Subjects
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == query.SubjectId &&
                x.TeacherId == teacherId);

        if (subject == null)
        {
            return NotFound(new
            {
                message = "Mata pelajaran tidak ditemukan."
            });
        }

        // -------------------------------------------------
        // VALIDATE SUBJECT ↔ CLASSROOM
        // -------------------------------------------------

        var subjectClassExists =
            await _db.SubjectClasses
                .AnyAsync(x =>
                    x.SubjectId == query.SubjectId &&
                    x.ClassroomId == query.ClassroomId);

        if (!subjectClassExists)
        {
            return BadRequest(new
            {
                message =
                    "Mata pelajaran tidak terhubung dengan kelas tersebut."
            });
        }

        // -------------------------------------------------
        // DATE RANGE
        // -------------------------------------------------

        var startDate = new DateTime(
            query.Year,
            query.Month,
            1);

        var endDate = startDate.AddMonths(1);

        // -------------------------------------------------
        // GET ATTENDANCES
        // -------------------------------------------------

        var attendances = await _db.Attendances
            .AsNoTracking()
            .Where(x =>
                x.TeacherId == teacherId &&
                x.ClassroomId == query.ClassroomId &&
                x.SubjectId == query.SubjectId &&
                x.Date >= startDate &&
                x.Date < endDate)
            .OrderBy(x => x.Date)
            .Select(x => new
            {
                x.Id,
                x.Date
            })
            .ToListAsync();

        // -------------------------------------------------
        // GET STUDENTS
        // -------------------------------------------------

        var students = await _db.Students
            .AsNoTracking()
            .Where(x =>
                x.ClassroomId == query.ClassroomId &&
                x.IsActive)
            .OrderBy(x => x.AttendanceNumber)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.NISN,
                x.AttendanceNumber
            })
            .ToListAsync();

        // -------------------------------------------------
        // GET RECORDS
        // -------------------------------------------------

        var attendanceIds =
            attendances
                .Select(x => x.Id)
                .ToList();

        var records = await _db.AttendanceDetails
            .AsNoTracking()
            .Where(x =>
                attendanceIds.Contains(
                    x.AttendanceId))
            .Select(x => new
            {
                x.AttendanceId,
                x.StudentId,
                x.Status
            })
            .ToListAsync();

        // -------------------------------------------------
        // BUILD MEETINGS
        // -------------------------------------------------

        var meetings = attendances
            .Select(x => new
            {
                attendanceId = x.Id,

                date = x.Date,

                dateLabel =
                    x.Date.ToString("dd/MM")
            })
            .ToList();

        // -------------------------------------------------
        // BUILD STUDENT RECAP
        // -------------------------------------------------

        var studentRecaps =
            students.Select(student =>
            {
                var attendanceList =
                    meetings.Select(meeting =>
                    {
                        var record =
                            records.FirstOrDefault(r =>
                                r.AttendanceId ==
                                    meeting.attendanceId &&
                                r.StudentId ==
                                    student.Id);

                        return new
                        {
                            attendanceId =
                                meeting.attendanceId,

                            date =
                                meeting.date,

                            status =
                                record?.Status
                                    .ToString(),

                            statusCode =
                                record == null
                                    ? (int?)null
                                    : (int)record.Status
                        };
                    }).ToList();

                return new
                {
                    studentId =
                        student.Id,

                    name =
                        student.Name,

                    nisn =
                        student.NISN,

                    attendanceNumber =
                        student.AttendanceNumber,

                    attendance =
                        attendanceList,

                    summary = new
                    {
                        present =
                            attendanceList.Count(x =>
                                x.status ==
                                    nameof(
                                        AttendanceStatus
                                            .Present)),

                        sick =
                            attendanceList.Count(x =>
                                x.status ==
                                    nameof(
                                        AttendanceStatus
                                            .Sick)),

                        permission =
                            attendanceList.Count(x =>
                                x.status ==
                                    nameof(
                                        AttendanceStatus
                                            .Permission)),

                        absent =
                            attendanceList.Count(x =>
                                x.status == null)
                    }
                };
            }).ToList();

        // -------------------------------------------------
        // RESPONSE
        // -------------------------------------------------

        return Ok(new
        {
            period = new
            {
                year = query.Year,
                month = query.Month,

                startDate,
                endDate =
                    endDate.AddDays(-1)
            },

            classroom = new
            {
                id = classroom.Id,
                name = classroom.Name
            },

            subject = new
            {
                id = subject.Id,
                name = subject.Name
            },

            meetings,

            students = studentRecaps,

            totalMeetings =
                meetings.Count
        });
    }

    // =====================================================
    // JOURNAL RECAP
    // =====================================================

    [HttpGet("journal")]
    public async Task<IActionResult> GetJournalRecap(
        [FromQuery] JournalRecapQuery query)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        // -------------------------------------------------
        // VALIDATE CLASSROOM
        // -------------------------------------------------

        var classroom = await _db.Classrooms
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == query.ClassroomId &&
                x.TeacherId == teacherId);

        if (classroom == null)
        {
            return NotFound(new
            {
                message = "Kelas tidak ditemukan."
            });
        }

        // -------------------------------------------------
        // VALIDATE SUBJECT
        // -------------------------------------------------

        var subject = await _db.Subjects
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == query.SubjectId &&
                x.TeacherId == teacherId);

        if (subject == null)
        {
            return NotFound(new
            {
                message =
                    "Mata pelajaran tidak ditemukan."
            });
        }

        // -------------------------------------------------
        // VALIDATE SUBJECT ↔ CLASSROOM
        // -------------------------------------------------

        var subjectClassExists =
            await _db.SubjectClasses
                .AnyAsync(x =>
                    x.SubjectId == query.SubjectId &&
                    x.ClassroomId == query.ClassroomId);

        if (!subjectClassExists)
        {
            return BadRequest(new
            {
                message =
                    "Mata pelajaran tidak terhubung dengan kelas tersebut."
            });
        }

        // -------------------------------------------------
        // DATE RANGE
        // -------------------------------------------------

        var startDate = new DateTime(
            query.Year,
            query.Month,
            1);

        var endDate =
            startDate.AddMonths(1);

        // -------------------------------------------------
        // GET JOURNALS
        // -------------------------------------------------

        var journals =
            await _db.Journals
                .AsNoTracking()
                .Where(x =>
                    x.TeacherId == teacherId &&
                    x.ClassroomId ==
                        query.ClassroomId &&
                    x.SubjectId ==
                        query.SubjectId &&
                    x.Date >= startDate &&
                    x.Date < endDate)
                .OrderBy(x => x.Date)
                .ThenBy(x => x.LessonPeriod)
                .Select(x => new
                {
                    id = x.Id,

                    date = x.Date,

                    lessonPeriod =
                        x.LessonPeriod,

                    competency =
                        x.CompetencyStandard,

                    material =
                        x.Material,

                    attendanceId =
                        x.AttendanceId,

                    createdAt =
                        x.CreatedAt,

                    updatedAt =
                        x.UpdatedAt
                })
                .ToListAsync();

        // -------------------------------------------------
        // RESPONSE
        // -------------------------------------------------

        return Ok(new
        {
            period = new
            {
                year = query.Year,
                month = query.Month,

                startDate,

                endDate =
                    endDate.AddDays(-1)
            },

            classroom = new
            {
                id = classroom.Id,
                name = classroom.Name
            },

            subject = new
            {
                id = subject.Id,
                name = subject.Name
            },

            totalJournals =
                journals.Count,

            journals
        });
    }

    // =====================================================
    // HELPER
    // =====================================================

    private int? GetTeacherId()
    {
        var claim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (int.TryParse(
            claim,
            out var teacherId))
        {
            return teacherId;
        }

        return null;
    }
}