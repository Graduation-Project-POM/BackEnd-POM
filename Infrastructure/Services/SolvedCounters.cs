using Microsoft.EntityFrameworkCore;

namespace MIO.Infrastructure.Services;

/// <summary>
/// Single definition of the "solved" counters: a problem counts as solved by a
/// student as soon as the student has submitted a solution to it (problem bank
/// or contest, published or hidden). Grading does not affect these counts.
/// </summary>
public static class SolvedCounters
{
    /// <summary>Problem.SolvedCount = distinct students who submitted a solution.</summary>
    public static async Task RecomputeProblemSolvedAsync(this AppDbContext db, int problemId)
    {
        var problem = await db.Problems.FindAsync(problemId);
        if (problem == null) return;

        var bankStudents = await db.ProblemBankSubmissions
            .Where(s => s.ProblemId == problemId).Select(s => s.StudentId).Distinct().ToListAsync();
        var contestStudents = await db.ProblemStudents
            .Where(s => s.ProblemId == problemId).Select(s => s.StudentId).Distinct().ToListAsync();

        problem.SolvedCount = bankStudents.Union(contestStudents).Count();
    }

    /// <summary>Student.ProblemSolved = distinct problems the student submitted a solution to.</summary>
    public static async Task RecomputeStudentSolvedAsync(this AppDbContext db, int studentId)
    {
        var student = await db.Students.FindAsync(studentId);
        if (student == null) return;

        var bankProblems = await db.ProblemBankSubmissions
            .Where(s => s.StudentId == studentId).Select(s => s.ProblemId).Distinct().ToListAsync();
        var contestProblems = await db.ProblemStudents
            .Where(s => s.StudentId == studentId).Select(s => s.ProblemId).Distinct().ToListAsync();

        student.ProblemSolved = bankProblems.Union(contestProblems).Count();
    }

    /// <summary>Recomputes ContestStudent.ProblemSolved for every enrollment in the given contests.</summary>
    public static async Task RecomputeContestSolvedAsync(this AppDbContext db, IEnumerable<int> contestIds)
    {
        foreach (var contestId in contestIds.Distinct())
        {
            var contestProblemIds = await db.Contests
                .Where(c => c.Id == contestId)
                .SelectMany(c => c.Problems.Select(p => p.ID))
                .ToListAsync();

            var enrollments = await db.ContestStudents.Where(cs => cs.ContestId == contestId).ToListAsync();
            foreach (var cs in enrollments)
                cs.ProblemSolved = await db.CountContestProblemsSubmittedAsync(cs.StudentId, contestProblemIds);
        }
    }

    /// <summary>
    /// ContestStudent.ProblemSolved = the contest's problems the student submitted a solution to.
    /// </summary>
    public static async Task<int> CountContestProblemsSubmittedAsync(
        this AppDbContext db, int studentId, IReadOnlyCollection<int> contestProblemIds)
    {
        if (contestProblemIds.Count == 0) return 0;
        return await db.ProblemStudents
            .Where(ps => ps.StudentId == studentId && contestProblemIds.Contains(ps.ProblemId))
            .Select(ps => ps.ProblemId)
            .Distinct()
            .CountAsync();
    }
}
