using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherJournal.Api.Data;
using TeacherJournal.Api.DTOs.Journal;

namespace TeacherJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JournalsController : ControllerBase
{
    private readonly AppDbContext _db;

    public JournalsController(AppDbContext db)
    {
        _db = db;
    }

    // =====================================================
    // GET ALL JOURNALS
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var journals = await _db.Journals
            .AsNoTracking()
            .Where(x => x.TeacherId == teacherId)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                id = x.Id,

                date = x.Date,

                classroom = new
                {
                    id = x.ClassroomId,
                    name = x.Classroom!.Name
                },

                subject = new
                {
                    id = x.SubjectId,
                    name = x.Subject!.Name
                },

                lessonPeriod = x.LessonPeriod,

                competency = x.CompetencyStandard,

                material = x.Material,

                attendanceId = x.AttendanceId,

                createdAt = x.CreatedAt,

                updatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(journals);
    }

    // =====================================================
    // GET JOURNAL DETAIL
    // =====================================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var journal = await _db.Journals
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.TeacherId == teacherId)
            .Select(x => new
            {
                id = x.Id,

                date = x.Date,

                classroom = new
                {
                    id = x.ClassroomId,
                    name = x.Classroom!.Name
                },

                subject = new
                {
                    id = x.SubjectId,
                    name = x.Subject!.Name
                },

                lessonPeriod = x.LessonPeriod,

                competency = x.CompetencyStandard,

                material = x.Material,

                attendanceId = x.AttendanceId,

                createdAt = x.CreatedAt,

                updatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (journal == null)
        {
            return NotFound(new
            {
                message =
                    "Data jurnal tidak ditemukan."
            });
        }

        return Ok(journal);
    }

    // =====================================================
    // CREATE JOURNAL
    // =====================================================

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateJournalRequest request)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        // -------------------------------------------------
        // VALIDATE REQUIRED TEXT
        // -------------------------------------------------

        var competency =
            request.Competency.Trim();

        if (string.IsNullOrWhiteSpace(competency))
        {
            return BadRequest(new
            {
                message =
                    "Standar kompetensi / kompetensi dasar wajib diisi."
            });
        }

        // -------------------------------------------------
        // NORMALIZE DATE
        // -------------------------------------------------

        var journalDate =
            request.Date.Date;

        // -------------------------------------------------
        // VALIDATE CLASSROOM
        // -------------------------------------------------

        var classroom =
            await _db.Classrooms
                .FirstOrDefaultAsync(x =>
                    x.Id == request.ClassroomId &&
                    x.TeacherId == teacherId);

        if (classroom == null)
        {
            return NotFound(new
            {
                message =
                    "Kelas tidak ditemukan."
            });
        }

        // -------------------------------------------------
        // VALIDATE SUBJECT
        // -------------------------------------------------

        var subject =
            await _db.Subjects
                .FirstOrDefaultAsync(x =>
                    x.Id == request.SubjectId &&
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
                    x.SubjectId ==
                        request.SubjectId &&
                    x.ClassroomId ==
                        request.ClassroomId);

        if (!subjectClassExists)
        {
            return BadRequest(new
            {
                message =
                    "Mata pelajaran tidak terhubung dengan kelas tersebut."
            });
        }

        // -------------------------------------------------
        // VALIDATE ATTENDANCE
        // -------------------------------------------------

        var attendance =
            await _db.Attendances
                .FirstOrDefaultAsync(x =>
                    x.Id ==
                        request.AttendanceId &&
                    x.TeacherId ==
                        teacherId);

        if (attendance == null)
        {
            return NotFound(new
            {
                message =
                    "Data absensi tidak ditemukan."
            });
        }

        // -------------------------------------------------
        // IMPORTANT:
        // ATTENDANCE MUST MATCH JOURNAL
        // -------------------------------------------------

        if (attendance.ClassroomId !=
                request.ClassroomId)
        {
            return BadRequest(new
            {
                message =
                    "Kelas pada jurnal tidak sesuai dengan kelas pada absensi."
            });
        }

        if (attendance.SubjectId !=
                request.SubjectId)
        {
            return BadRequest(new
            {
                message =
                    "Mata pelajaran pada jurnal tidak sesuai dengan absensi."
            });
        }

        if (attendance.Date.Date !=
                journalDate)
        {
            return BadRequest(new
            {
                message =
                    "Tanggal jurnal tidak sesuai dengan tanggal absensi."
            });
        }

        // -------------------------------------------------
        // PREVENT DUPLICATE JOURNAL
        // -------------------------------------------------

        var duplicate =
            await _db.Journals.AnyAsync(x =>
                x.TeacherId == teacherId &&
                x.ClassroomId ==
                    request.ClassroomId &&
                x.SubjectId ==
                    request.SubjectId &&
                x.Date == journalDate);

        if (duplicate)
        {
            return Conflict(new
            {
                message =
                    "Jurnal untuk kelas, mata pelajaran, dan tanggal tersebut sudah ada."
            });
        }

        // -------------------------------------------------
        // CREATE JOURNAL
        // -------------------------------------------------

        var journal = new Models.Journal
        {
            TeacherId = teacherId.Value,

            ClassroomId =
                request.ClassroomId,

            SubjectId =
                request.SubjectId,

            Date =
                journalDate,

            LessonPeriod =
                string.IsNullOrWhiteSpace(
                    request.LessonPeriod)
                    ? null
                    : request.LessonPeriod.Trim(),

            CompetencyStandard =
                competency,

            Material =
                string.IsNullOrWhiteSpace(
                    request.Material)
                    ? null
                    : request.Material.Trim(),

            AttendanceId =
                request.AttendanceId,

            CreatedAt =
                DateTime.UtcNow,

            UpdatedAt =
                DateTime.UtcNow
        };

        _db.Journals.Add(journal);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = journal.Id },
            new
            {
                id = journal.Id,
                message =
                    "Jurnal berhasil dibuat."
            });
    }

    // =====================================================
    // UPDATE JOURNAL
    // =====================================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateJournalRequest request)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var journal =
            await _db.Journals
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.TeacherId == teacherId);

        if (journal == null)
        {
            return NotFound(new
            {
                message =
                    "Data jurnal tidak ditemukan."
            });
        }

        // -------------------------------------------------
        // REQUIRED TEXT
        // -------------------------------------------------

        var competency =
            request.Competency.Trim();

        if (string.IsNullOrWhiteSpace(competency))
        {
            return BadRequest(new
            {
                message =
                    "Standar kompetensi / kompetensi dasar wajib diisi."
            });
        }

        var journalDate =
            request.Date.Date;

        // -------------------------------------------------
        // CLASSROOM
        // -------------------------------------------------

        var classroom =
            await _db.Classrooms
                .AnyAsync(x =>
                    x.Id ==
                        request.ClassroomId &&
                    x.TeacherId ==
                        teacherId);

        if (!classroom)
        {
            return NotFound(new
            {
                message =
                    "Kelas tidak ditemukan."
            });
        }

        // -------------------------------------------------
        // SUBJECT
        // -------------------------------------------------

        var subject =
            await _db.Subjects
                .AnyAsync(x =>
                    x.Id ==
                        request.SubjectId &&
                    x.TeacherId ==
                        teacherId);

        if (!subject)
        {
            return NotFound(new
            {
                message =
                    "Mata pelajaran tidak ditemukan."
            });
        }

        // -------------------------------------------------
        // SUBJECT ↔ CLASSROOM
        // -------------------------------------------------

        var subjectClassExists =
            await _db.SubjectClasses
                .AnyAsync(x =>
                    x.SubjectId ==
                        request.SubjectId &&
                    x.ClassroomId ==
                        request.ClassroomId);

        if (!subjectClassExists)
        {
            return BadRequest(new
            {
                message =
                    "Mata pelajaran tidak terhubung dengan kelas tersebut."
            });
        }

        // -------------------------------------------------
        // ATTENDANCE
        // -------------------------------------------------

        var attendance =
            await _db.Attendances
                .FirstOrDefaultAsync(x =>
                    x.Id ==
                        request.AttendanceId &&
                    x.TeacherId ==
                        teacherId);

        if (attendance == null)
        {
            return NotFound(new
            {
                message =
                    "Data absensi tidak ditemukan."
            });
        }

        // -------------------------------------------------
        // ATTENDANCE MATCH
        // -------------------------------------------------

        if (attendance.ClassroomId !=
                request.ClassroomId)
        {
            return BadRequest(new
            {
                message =
                    "Kelas pada jurnal tidak sesuai dengan kelas pada absensi."
            });
        }

        if (attendance.SubjectId !=
                request.SubjectId)
        {
            return BadRequest(new
            {
                message =
                    "Mata pelajaran pada jurnal tidak sesuai dengan absensi."
            });
        }

        if (attendance.Date.Date !=
                journalDate)
        {
            return BadRequest(new
            {
                message =
                    "Tanggal jurnal tidak sesuai dengan tanggal absensi."
            });
        }

        // -------------------------------------------------
        // DUPLICATE
        // -------------------------------------------------

        var duplicate =
            await _db.Journals.AnyAsync(x =>
                x.Id != id &&
                x.TeacherId == teacherId &&
                x.ClassroomId ==
                    request.ClassroomId &&
                x.SubjectId ==
                    request.SubjectId &&
                x.Date == journalDate);

        if (duplicate)
        {
            return Conflict(new
            {
                message =
                    "Jurnal untuk kelas, mata pelajaran, dan tanggal tersebut sudah ada."
            });
        }

        // -------------------------------------------------
        // UPDATE
        // -------------------------------------------------

        journal.ClassroomId =
            request.ClassroomId;

        journal.SubjectId =
            request.SubjectId;

        journal.Date =
            journalDate;

        journal.LessonPeriod =
            string.IsNullOrWhiteSpace(
                request.LessonPeriod)
                ? null
                : request.LessonPeriod.Trim();

        journal.CompetencyStandard =
            competency;

        journal.Material =
            string.IsNullOrWhiteSpace(
                request.Material)
                ? null
                : request.Material.Trim();

        journal.AttendanceId =
            request.AttendanceId;

        journal.UpdatedAt =
            DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Jurnal berhasil diperbarui."
        });
    }

    // =====================================================
    // DELETE JOURNAL
    // =====================================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var journal =
            await _db.Journals
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.TeacherId == teacherId);

        if (journal == null)
        {
            return NotFound(new
            {
                message =
                    "Data jurnal tidak ditemukan."
            });
        }

        _db.Journals.Remove(journal);

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // =====================================================
    // GET AVAILABLE ATTENDANCES FOR JOURNAL
    // =====================================================

    [HttpGet("available-attendances")]
    public async Task<IActionResult>
        GetAvailableAttendances(
            [FromQuery] int classroomId,
            [FromQuery] int subjectId,
            [FromQuery] DateTime date)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var targetDate = date.Date;

        var attendances =
            await _db.Attendances
                .AsNoTracking()
                .Where(x =>
                    x.TeacherId ==
                        teacherId &&
                    x.ClassroomId ==
                        classroomId &&
                    x.SubjectId ==
                        subjectId &&
                    x.Date == targetDate)
                .Select(x => new
                {
                    id = x.Id,
                    date = x.Date,

                    classroom = new
                    {
                        id = x.ClassroomId,
                        name = x.Classroom!.Name
                    },

                    subject = new
                    {
                        id = x.SubjectId,
                        name = x.Subject!.Name
                    },

                    presentCount =
                        x.Details.Count(
                            r => r.Status ==
                                Models.AttendanceStatus.Present),

                    sickCount =
                        x.Details.Count(
                            r => r.Status ==
                                Models.AttendanceStatus.Sick),

                    permissionCount =
                        x.Details.Count(
                            r => r.Status ==
                                Models.AttendanceStatus.Permission)
                })
                .ToListAsync();

        return Ok(attendances);
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