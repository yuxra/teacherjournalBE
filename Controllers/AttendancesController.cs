using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherJournal.Api.Data;
using TeacherJournal.Api.DTOs.Attendance;
using TeacherJournal.Api.Models;

namespace TeacherJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendancesController : ControllerBase
{
    private readonly AppDbContext _db;

    public AttendancesController(AppDbContext db)
    {
        _db = db;
    }

    // =====================================================
    // GET ALL ATTENDANCES
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var attendances = await _db.Attendances
            .AsNoTracking()
            .Where(x => x.TeacherId == teacherId)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                id = x.Id,

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

                date = x.Date,

                createdAt = x.CreatedAt,

                presentCount =
                    x.Details.Count(r =>
                        r.Status == AttendanceStatus.Present),

                sickCount =
                    x.Details.Count(r =>
                        r.Status == AttendanceStatus.Sick),

                permissionCount =
                    x.Details.Count(r =>
                        r.Status == AttendanceStatus.Permission),

                totalStudents =
                    x.Details.Count()
            })
            .ToListAsync();

        return Ok(attendances);
    }

    // =====================================================
    // GET ATTENDANCE DETAIL
    // =====================================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var attendance = await _db.Attendances
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.TeacherId == teacherId)
            .Select(x => new
            {
                id = x.Id,

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

                date = x.Date,

                createdAt = x.CreatedAt,
                updatedAt = x.UpdatedAt,

                students = x.Details
                    .OrderBy(r =>
                        r.Student!.AttendanceNumber)
                    .Select(r => new
                    {
                        studentId = r.StudentId,

                        name = r.Student!.Name,

                        nisn = r.Student.NISN,

                        attendanceNumber =
                            r.Student.AttendanceNumber,

                        status = r.Status.ToString()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (attendance == null)
        {
            return NotFound(new
            {
                message =
                    "Data absensi tidak ditemukan."
            });
        }

        return Ok(attendance);
    }

    // =====================================================
    // CREATE ATTENDANCE
    // =====================================================

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateAttendanceRequest request)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        // -------------------------------------------------
        // VALIDATE CLASSROOM
        // -------------------------------------------------

        var classroom = await _db.Classrooms
            .FirstOrDefaultAsync(x =>
                x.Id == request.ClassroomId &&
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
            await _db.SubjectClasses.AnyAsync(x =>
                x.SubjectId == request.SubjectId &&
                x.ClassroomId == request.ClassroomId);

        if (!subjectClassExists)
        {
            return BadRequest(new
            {
                message =
                    "Mata pelajaran tidak terhubung dengan kelas tersebut."
            });
        }

        // -------------------------------------------------
        // NORMALIZE DATE
        // -------------------------------------------------

        var attendanceDate =
            request.Date.Date;

        // -------------------------------------------------
        // PREVENT DUPLICATE ATTENDANCE
        // -------------------------------------------------

        var duplicate =
            await _db.Attendances.AnyAsync(x =>
                x.TeacherId == teacherId &&
                x.ClassroomId ==
                    request.ClassroomId &&
                x.SubjectId ==
                    request.SubjectId &&
                x.Date == attendanceDate);

        if (duplicate)
        {
            return Conflict(new
            {
                message =
                    "Absensi untuk kelas, mata pelajaran, dan tanggal tersebut sudah ada."
            });
        }

        // -------------------------------------------------
        // GET ACTIVE STUDENTS
        // -------------------------------------------------

        var students = await _db.Students
            .Where(x =>
                x.ClassroomId ==
                    request.ClassroomId &&
                x.IsActive)
            .ToListAsync();

        if (students.Count == 0)
        {
            return BadRequest(new
            {
                message =
                    "Kelas tersebut belum memiliki siswa aktif."
            });
        }

        // -------------------------------------------------
        // VALIDATE STUDENT IDS
        // -------------------------------------------------

        var requestedStudentIds =
            request.Students
                .Select(x => x.StudentId)
                .ToList();

        var duplicateStudentIds =
            requestedStudentIds
                .GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

        if (duplicateStudentIds.Any())
        {
            return BadRequest(new
            {
                message =
                    "Terdapat siswa yang dikirim lebih dari satu kali."
            });
        }

        var invalidStudentIds =
            requestedStudentIds
                .Except(
                    students.Select(x => x.Id))
                .ToList();

        if (invalidStudentIds.Any())
        {
            return BadRequest(new
            {
                message =
                    "Terdapat siswa yang bukan bagian dari kelas tersebut.",
                studentIds = invalidStudentIds
            });
        }

        // -------------------------------------------------
        // ENSURE ALL STUDENTS HAVE STATUS
        // -------------------------------------------------

        var missingStudentIds =
            students
                .Select(x => x.Id)
                .Except(requestedStudentIds)
                .ToList();

        if (missingStudentIds.Any())
        {
            return BadRequest(new
            {
                message =
                    "Semua siswa aktif harus memiliki status kehadiran.",
                studentIds = missingStudentIds
            });
        }

        // -------------------------------------------------
        // CREATE ATTENDANCE
        // -------------------------------------------------

        var attendance = new Attendance
        {
            TeacherId = teacherId.Value,
            ClassroomId = request.ClassroomId,
            SubjectId = request.SubjectId,
            Date = attendanceDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var item in request.Students)
        {
            attendance.Details.Add(
                new AttendanceDetail
                {
                    StudentId = item.StudentId,
                    Status = item.Status
                });
        }

        _db.Attendances.Add(attendance);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = attendance.Id },
            new
            {
                id = attendance.Id,
                message =
                    "Absensi berhasil dibuat."
            });
    }

    // =====================================================
    // UPDATE ATTENDANCE
    // =====================================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateAttendanceRequest request)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var attendance = await _db.Attendances
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TeacherId == teacherId);

        if (attendance == null)
        {
            return NotFound(new
            {
                message =
                    "Data absensi tidak ditemukan."
            });
        }

        var attendanceDate =
            request.Date.Date;

        // -------------------------------------------------
        // PREVENT DATE DUPLICATION
        // -------------------------------------------------

        var duplicate =
            await _db.Attendances.AnyAsync(x =>
                x.Id != id &&
                x.TeacherId == teacherId &&
                x.ClassroomId ==
                    attendance.ClassroomId &&
                x.SubjectId ==
                    attendance.SubjectId &&
                x.Date == attendanceDate);

        if (duplicate)
        {
            return Conflict(new
            {
                message =
                    "Absensi dengan kelas, mapel, dan tanggal tersebut sudah ada."
            });
        }

        // -------------------------------------------------
        // GET STUDENTS
        // -------------------------------------------------

        var students = await _db.Students
            .Where(x =>
                x.ClassroomId ==
                    attendance.ClassroomId &&
                x.IsActive)
            .ToListAsync();

        var requestedStudentIds =
            request.Students
                .Select(x => x.StudentId)
                .ToList();

        // Duplicate student
        var duplicateStudentIds =
            requestedStudentIds
                .GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

        if (duplicateStudentIds.Any())
        {
            return BadRequest(new
            {
                message =
                    "Terdapat siswa yang dikirim lebih dari satu kali."
            });
        }

        // Student bukan bagian kelas
        var invalidStudentIds =
            requestedStudentIds
                .Except(
                    students.Select(x => x.Id))
                .ToList();

        if (invalidStudentIds.Any())
        {
            return BadRequest(new
            {
                message =
                    "Terdapat siswa yang bukan bagian dari kelas tersebut.",
                studentIds = invalidStudentIds
            });
        }

        // Missing student
        var missingStudentIds =
            students
                .Select(x => x.Id)
                .Except(requestedStudentIds)
                .ToList();

        if (missingStudentIds.Any())
        {
            return BadRequest(new
            {
                message =
                    "Semua siswa aktif harus memiliki status kehadiran.",
                studentIds = missingStudentIds
            });
        }

        // -------------------------------------------------
        // UPDATE
        // -------------------------------------------------

        attendance.Date = attendanceDate;
        attendance.UpdatedAt = DateTime.UtcNow;

        // Hapus record lama
        _db.AttendanceDetails.RemoveRange(
            attendance.Details);

        // Tambahkan record baru
        foreach (var item in request.Students)
        {
            attendance.Details.Add(
                new AttendanceDetail
                {
                    AttendanceId = attendance.Id,
                    StudentId = item.StudentId,
                    Status = item.Status
                });
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Absensi berhasil diperbarui."
        });
    }

    // =====================================================
    // DELETE ATTENDANCE
    // =====================================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var attendance = await _db.Attendances
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TeacherId == teacherId);

        if (attendance == null)
        {
            return NotFound(new
            {
                message =
                    "Data absensi tidak ditemukan."
            });
        }

        // -------------------------------------------------
        // CHECK JOURNAL REFERENCE
        // -------------------------------------------------

        var usedByJournal =
            await _db.Journals
                .AnyAsync(x =>
                    x.AttendanceId == id);

        if (usedByJournal)
        {
            return Conflict(new
            {
                message =
                    "Absensi tidak dapat dihapus karena sudah digunakan oleh jurnal."
            });
        }

        _db.Attendances.Remove(attendance);

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // =====================================================
    // GET NEXT ATTENDANCE NUMBER
    // =====================================================

    [HttpGet(
        "/api/classrooms/{classroomId:int}/students/next-number")]
    public async Task<IActionResult> GetNextNumber(
        int classroomId)
    {
        var teacherId = GetTeacherId();

        if (teacherId == null)
            return Unauthorized();

        var classroomExists =
            await _db.Classrooms.AnyAsync(x =>
                x.Id == classroomId &&
                x.TeacherId == teacherId);

        if (!classroomExists)
        {
            return NotFound(new
            {
                message =
                    "Kelas tidak ditemukan."
            });
        }

        var lastNumber =
            await _db.Students
                .Where(x =>
                    x.ClassroomId ==
                        classroomId &&
                    x.IsActive)
                .Select(x =>
                    (int?)x.AttendanceNumber)
                .MaxAsync();

        var nextNumber =
            (lastNumber ?? 0) + 1;

        return Ok(new
        {
            nextNumber
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