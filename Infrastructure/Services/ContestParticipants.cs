using Microsoft.EntityFrameworkCore;

namespace MIO.Infrastructure.Services;

// A contest's participants are the distinct students who submitted at least one
// solution (ProblemStudents) to a problem in that contest. Submissions only link
// to a contest through its problems (ContestProblem). Only students in the
// contest's pool count — members of a linked group or enrolled directly via
// ContestStudents — the same pool the leaderboard ranks and that may open the
// contest's problems; SubmitSolution itself doesn't check enrollment.
// Problem-bank solutions live in ProblemBankSubmissions and never count.
public static class ContestParticipants
{
    public static async Task<Dictionary<int, int>> CountByContestAsync(
        this AppDbContext db, IReadOnlyCollection<int> contestIds)
    {
        if (contestIds.Count == 0)
            return new Dictionary<int, int>();

        return await db.Contests
            .Where(c => contestIds.Contains(c.Id))
            .SelectMany(c => c.Problems, (c, p) => new { ContestId = c.Id, ProblemId = p.ID })
            .Join(db.ProblemStudents,
                cp => cp.ProblemId,
                ps => ps.ProblemId,
                (cp, ps) => new { cp.ContestId, ps.StudentId })
            .Where(x =>
                db.ContestStudents.Any(cs => cs.ContestId == x.ContestId && cs.StudentId == x.StudentId) ||
                db.GroupContests.Any(gc => gc.ContestId == x.ContestId &&
                                           gc.Group.GroupStudents.Any(gs => gs.StudentId == x.StudentId)))
            .GroupBy(x => x.ContestId)
            // Distinct: several submissions (to different problems) by one
            // student are one participant.
            .Select(g => new { ContestId = g.Key, Count = g.Select(x => x.StudentId).Distinct().Count() })
            .ToDictionaryAsync(x => x.ContestId, x => x.Count);
    }

    public static async Task<int> CountForContestAsync(this AppDbContext db, int contestId)
        => (await db.CountByContestAsync([contestId])).GetValueOrDefault(contestId);
}
