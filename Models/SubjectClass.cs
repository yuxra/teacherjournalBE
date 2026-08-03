namespace TeacherJournal.Api.Models;

public class SubjectClass
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public int ClassroomId { get; set; }

    // Relationships
    public Subject? Subject { get; set; }

    public Classroom? Classroom { get; set; }
}