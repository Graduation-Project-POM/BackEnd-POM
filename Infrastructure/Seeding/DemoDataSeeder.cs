using System.Text.Json;
using Regex = System.Text.RegularExpressions.Regex;
using RegexOptions = System.Text.RegularExpressions.RegexOptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MIO.Core.DTOs.ProblemDTOs;
using MIO.Core.Entities;
using MIO.Core.Enums;
using MIO.Infrastructure.Services;
using MIO2.Core.Entities;

namespace MIO.Infrastructure.Seeding;

/// <summary>
/// Opt-in demo data seeder (run with: dotnet run -- --seed-demo).
///
/// - Insert-only: never updates or deletes existing rows. Every record is looked up
///   by a natural key (email, group name, contest title, problem title, ...) first,
///   so running it again adds nothing.
/// - Refuses to run against a non-local SQL Server unless --allow-remote is passed.
/// - Counters (solved counts, total points, contest scores) are recomputed with the
///   same rules the application uses, for the seeded records only.
/// </summary>
public static class DemoDataSeeder
{
    // Outcome per position in a contest's rotated member list:
    // 0 = full solution, 1 = full (minor issue), 2 = partial, 3 = weak attempt, -1 = no submission.
    private static readonly int[] EasyOutcomes = [0, 1, 0, 2, 1, 3, -1, 2];
    private static readonly int[] MediumOutcomes = [0, 2, 1, 3, 2, -1, 3, -1];
    private static readonly int[] HardOutcomes = [0, 2, 1, 3, -1, -1, -1, 3];

    public static async Task RunAsync(IServiceProvider services, bool allowRemote, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var connection = new SqlConnectionStringBuilder(db.Database.GetConnectionString());
        if (!allowRemote && !IsLocalServer(connection.DataSource))
            throw new InvalidOperationException(
                $"Refusing to seed demo data into '{connection.DataSource}'. Point ConnectionStrings__DefaultConnection " +
                "at a local database, or pass --allow-remote if you really mean to seed this server.");

        logger.LogInformation("Seeding demo data into {Server}/{Database}", connection.DataSource, connection.InitialCatalog);

        await using var tx = await db.Database.BeginTransactionAsync();
        var stats = new Dictionary<string, int>();
        void Added(string what) => stats[what] = stats.GetValueOrDefault(what) + 1;

        var now = DateTime.UtcNow;
        var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);

        // ── Users ──
        var userIds = new Dictionary<string, int>();
        var trainerIds = new Dictionary<string, int>();
        var studentIds = new Dictionary<string, int>();

        async Task<User> EnsureUser(SeedUser su, Role role)
        {
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == su.Email || u.UserName == su.UserName);
            if (existing != null) return existing;

            Security.CreatePasswordHash(DemoSeedContent.DemoPassword, out var hash, out var salt);
            var created = now.AddDays(-su.CreatedDaysAgo);
            var user = new User
            {
                FirstName = su.First, MiddleName = su.Middle, LastName = su.Last,
                UserName = su.UserName, Email = su.Email,
                PasswordHash = hash, PasswordSalt = salt,
                PhoneNumber = PhoneNumbers.Normalize(su.Phone),
                Gender = su.Gender, ProfileImageUrl = "", Bio = su.Bio,
                IsActive = true, CreatedAt = created, LastUpdatedAt = created, RoleID = role,
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            Added("Users");
            return user;
        }

        foreach (var a in DemoSeedContent.Admins)
            userIds[a.Key] = (await EnsureUser(a, Role.Admin)).Id;

        foreach (var t in DemoSeedContent.Trainers)
        {
            var user = await EnsureUser(t.User, Role.Trainer);
            userIds[t.User.Key] = user.Id;
            var trainer = await db.Trainers.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (trainer == null)
            {
                trainer = new Trainer { UserId = user.Id, Specialization = t.Specialization, IsAccepted = t.Accepted };
                db.Trainers.Add(trainer);
                await db.SaveChangesAsync();
                Added("Trainers");
            }
            trainerIds[t.User.Key] = trainer.Id;
        }

        foreach (var s in DemoSeedContent.Students)
        {
            var user = await EnsureUser(s.User, Role.Student);
            userIds[s.User.Key] = user.Id;
            var student = await db.Students.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (student == null)
            {
                student = new Student
                {
                    UserId = user.Id, GradeLevel = s.GradeLevel, City = s.City, SchoolName = s.School,
                    NationalId = s.NationalId, IsAccepted = s.Accepted, JoinYear = new DateOnly(s.JoinYear, 9, 1),
                };
                db.Students.Add(student);
                await db.SaveChangesAsync();
                Added("Students");
            }
            studentIds[s.User.Key] = student.Id;
        }

        // ── Groups & enrollments ──
        var groups = new Dictionary<string, Group>();
        foreach (var g in DemoSeedContent.Groups)
        {
            var group = await db.Groups.FirstOrDefaultAsync(x => x.Name == g.Name);
            var startDate = DateOnly.FromDateTime(today.AddDays(-g.StartDaysAgo));
            if (group == null)
            {
                group = new Group { Name = g.Name, Description = g.Description, TrainerId = trainerIds[g.TrainerKey], StartDate = startDate };
                db.Groups.Add(group);
                await db.SaveChangesAsync();
                Added("Groups");
            }
            groups[g.Key] = group;

            for (var i = 0; i < g.StudentKeys.Length; i++)
            {
                var sid = studentIds[g.StudentKeys[i]];
                if (await db.GroupStudents.AnyAsync(x => x.GroupId == group.Id && x.StudentId == sid)) continue;
                db.GroupStudents.Add(new GroupStudent { GroupId = group.Id, StudentId = sid, JoinDate = startDate.AddDays(i % 4) });
                Added("GroupStudents");
            }
        }
        await db.SaveChangesAsync();

        // ── Problems ──
        var problems = new Dictionary<string, Problem>();
        var seedProblems = new Dictionary<string, SeedProblem>();

        async Task<Problem> EnsureProblem(SeedProblem sp, bool isBank, DateTime createdAt)
        {
            seedProblems[sp.Key] = sp;
            var title = Bidi(sp.Title);
            var existing = await db.Problems.FirstOrDefaultAsync(p => p.Title == title && p.IsBankProblem == isBank);
            if (existing != null) return existing;

            var problem = new Problem
            {
                Title = title, Statement = Bidi(sp.Statement), Constraints = BidiOrNull(sp.Constraints),
                Examples = sp.Examples.Length == 0 ? null : JsonSerializer.Serialize(
                    sp.Examples.Select(e => new ProblemExampleDto { Input = Bidi(e.Input), Output = Bidi(e.Output), Explanation = BidiOrNull(e.Explanation) }).ToList()),
                Difficulty = sp.Difficulty, Topic = sp.Topic, Points = 100,
                TrainerID = trainerIds[sp.TrainerKey],
                IsPublished = sp.Published, IsBankProblem = isBank, IsBankVisible = isBank,
                CreatedAt = createdAt, UpdatedAt = createdAt,
            };
            db.Problems.Add(problem);
            await db.SaveChangesAsync();
            Added(isBank ? "Bank problems" : "Contest problems");
            return problem;
        }

        for (var i = 0; i < DemoSeedContent.BankProblems.Length; i++)
        {
            var bp = DemoSeedContent.BankProblems[i];
            problems[bp.Key] = await EnsureProblem(bp, true, now.AddDays(-(64 - i * 3)));
        }

        // ── Contests ──
        var contests = new List<(SeedContest Seed, Contest Entity)>();
        foreach (var sc in DemoSeedContent.Contests)
        {
            var startAt = today.AddDays(sc.StartDayOffset).AddHours(sc.StartHourUtc);
            var endAt = today.AddDays(sc.EndDayOffset).AddHours(sc.EndHourUtc);

            var contestProblems = new List<Problem>();
            foreach (var pk in sc.ProblemKeys)
            {
                var sp = DemoSeedContent.ContestProblems.Single(p => p.Key == pk);
                var problem = await EnsureProblem(sp, false, startAt.AddDays(-10));
                problems[pk] = problem;
                contestProblems.Add(problem);
            }

            var group = groups[sc.GroupKey];
            var contest = await db.Contests.Include(c => c.Problems).FirstOrDefaultAsync(c => c.Title == sc.Title);
            if (contest == null)
            {
                contest = new Contest
                {
                    Title = sc.Title, Description = Bidi(sc.Description), TrainerId = group.TrainerId,
                    Difficulty = sc.Difficulty, StartAt = startAt, EndAt = endAt, Problems = contestProblems,
                    // Stored for completeness; the UI derives the status from the dates.
                    Status = now < startAt ? ContestStatus.Upcoming : now <= endAt ? ContestStatus.Running : ContestStatus.Ended,
                };
                db.Contests.Add(contest);
                await db.SaveChangesAsync();
                Added("Contests");
            }
            if (!await db.GroupContests.AnyAsync(x => x.GroupId == group.Id && x.ContestId == contest.Id))
            {
                var addDate = DateOnly.FromDateTime(contest.StartAt.AddDays(-7));
                db.GroupContests.Add(new GroupContest
                {
                    GroupId = group.Id, ContestId = contest.Id,
                    AddDate = addDate > DateOnly.FromDateTime(today) ? DateOnly.FromDateTime(today) : addDate,
                });
                await db.SaveChangesAsync();
                Added("GroupContests");
            }
            contests.Add((sc, contest));
        }

        // ── Contest submissions & grading ──
        for (var ci = 0; ci < contests.Count; ci++)
        {
            var (sc, contest) = contests[ci];
            if (now < contest.StartAt) continue; // upcoming: nothing submitted yet

            var ended = now > contest.EndAt;
            var running = !ended;
            var members = DemoSeedContent.Groups.Single(g => g.Key == sc.GroupKey).StudentKeys;
            var rotation = ci % members.Length;

            for (var pi = 0; pi < sc.ProblemKeys.Length; pi++)
            {
                var sp = seedProblems[sc.ProblemKeys[pi]];
                var problem = problems[sp.Key];
                var outcomes = sp.Difficulty switch
                {
                    Difficulty.Easy => EasyOutcomes,
                    Difficulty.Medium => MediumOutcomes,
                    _ => HardOutcomes,
                };

                for (var r = 0; r < members.Length; r++)
                {
                    var outcome = outcomes[r % outcomes.Length];
                    if (outcome < 0) continue;

                    var sid = studentIds[members[(r + rotation) % members.Length]];
                    if (await db.ProblemStudents.AnyAsync(x => x.ProblemId == problem.ID && x.StudentId == sid)) continue;

                    var variant = sp.Variants[outcome];
                    var submittedAt = running
                        ? contest.StartAt.AddHours(2 + 5 * pi + 3 * r)
                        : contest.StartAt.AddMinutes(25 + 35 * pi + 6 * r);
                    if (submittedAt >= now) continue;

                    db.ProblemStudents.Add(new ProblemStudent
                    {
                        ProblemId = problem.ID, StudentId = sid,
                        SubmittedSolutionText = Bidi(variant.Text), SubmittedAt = submittedAt,
                        // Grading is only possible after the contest ends (same rule as the API).
                        Score = ended ? variant.Score : null,
                        Feedback = ended ? Bidi(variant.Feedback) : null,
                        GradedAt = ended ? contest.EndAt.AddDays(1).AddMinutes(35 * r + 10 * pi) : null,
                        IsPublished = true,
                    });
                    Added(ended ? "Graded contest submissions" : "Ungraded contest submissions");
                }
            }
            await db.SaveChangesAsync();
        }

        // ── Contest enrollments (ContestStudents) ──
        foreach (var (sc, contest) in contests)
        {
            foreach (var key in DemoSeedContent.Groups.Single(g => g.Key == sc.GroupKey).StudentKeys)
            {
                var sid = studentIds[key];
                if (await db.ContestStudents.AnyAsync(x => x.ContestId == contest.Id && x.StudentId == sid)) continue;
                db.ContestStudents.Add(new ContestStudent { ContestId = contest.Id, StudentId = sid });
                Added("ContestStudents");
            }
        }
        await db.SaveChangesAsync();

        // ── Problem bank submissions ──
        foreach (var bs in DemoSeedContent.BankSubmissions)
        {
            var problem = problems[bs.ProblemKey];
            var sid = studentIds[bs.StudentKey];
            if (await db.ProblemBankSubmissions.AnyAsync(x => x.ProblemId == problem.ID && x.StudentId == sid)) continue;
            db.ProblemBankSubmissions.Add(new ProblemBankSubmission
            {
                ProblemId = problem.ID, StudentId = sid,
                SolutionText = Bidi(seedProblems[bs.ProblemKey].Variants[bs.VariantIndex].Text),
                SubmittedAt = now.AddDays(-bs.DaysAgo).AddHours(-3), IsPublished = bs.Published,
            });
            Added("Bank submissions");
        }
        await db.SaveChangesAsync();

        // ── Discussions ──
        var commentIds = new Dictionary<string, int>();
        for (var i = 0; i < DemoSeedContent.Comments.Length; i++)
        {
            var c = DemoSeedContent.Comments[i];
            var problem = problems[c.ProblemKey];
            var authorId = userIds[c.AuthorKey];
            var body = Bidi(c.Body);
            var existing = await db.ProblemComments.FirstOrDefaultAsync(x =>
                x.ProblemId == problem.ID && x.UserId == authorId && x.Body == body);
            if (existing == null)
            {
                existing = new ProblemComment
                {
                    ProblemId = problem.ID, UserId = authorId, Body = body,
                    ParentCommentId = c.ParentKey == null ? null : commentIds[c.ParentKey],
                    CreatedAt = now.AddDays(-c.DaysAgo).AddMinutes(i * 7 - 600),
                };
                db.ProblemComments.Add(existing);
                await db.SaveChangesAsync();
                Added("Discussion comments");
            }
            commentIds[c.Key] = existing.Id;
        }

        // ── Counters (same formulas as ProblemService.RecomputeAggregatesAsync / SolvedCounters) ──
        foreach (var problem in problems.Values)
            await db.RecomputeProblemSolvedAsync(problem.ID);

        foreach (var sid in studentIds.Values)
        {
            var student = await db.Students.FindAsync(sid);
            student!.TotalPoints = await db.ProblemStudents
                .Where(ps => ps.StudentId == sid && ps.Score != null)
                .SumAsync(ps => (int?)ps.Score) ?? 0;
            await db.RecomputeStudentSolvedAsync(sid);
        }

        foreach (var (_, contest) in contests)
        {
            var problemIds = await db.Contests.Where(c => c.Id == contest.Id)
                .SelectMany(c => c.Problems.Select(p => p.ID)).ToListAsync();
            var rows = await db.ContestStudents.Where(cs => cs.ContestId == contest.Id).ToListAsync();
            foreach (var cs in rows)
            {
                var subs = db.ProblemStudents.Where(ps => ps.StudentId == cs.StudentId && problemIds.Contains(ps.ProblemId));
                cs.Score = await subs.Where(ps => ps.Score != null).SumAsync(ps => (int?)ps.Score) ?? 0;
                cs.ProblemSolved = await db.CountContestProblemsSubmittedAsync(cs.StudentId, problemIds);
                var last = await subs.MaxAsync(ps => (DateTime?)ps.SubmittedAt);
                cs.TimeToFinishTheContest = last == null ? 0 : Math.Round((last.Value - contest.StartAt).TotalMinutes, 1);
            }
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        if (stats.Count == 0)
            logger.LogInformation("Demo data already present; nothing added.");
        foreach (var (what, count) in stats)
            logger.LogInformation("Added {Count} {What}", count, what);
    }

    // In right-to-left Arabic text the browser's bidi algorithm reorders runs of
    // numbers and operators, e.g. "(5, 3)" is displayed as "(3 ,5)" and
    // "100 = 6 · 16 + 4" as "4 + 16 · 6 = 100". Each math run is therefore wrapped
    // in an invisible left-to-right isolate (U+2066 … U+2069) so it always reads
    // exactly as written, without any change to the application's rendering code.
    private static readonly Regex NonArabicRun = new(@"[^؀-ۿ\n]+", RegexOptions.Compiled);
    private static readonly Regex MathChar = new(@"[0-9A-Za-z\p{Sm}∠√π²³⁴⁵⁶⁸ⁱⁿℝℤℚ∎·−]", RegexOptions.Compiled);
    private const char Lri = '⁦', Pdi = '⁩';

    internal static string Bidi(string text) => NonArabicRun.Replace(text, m =>
    {
        var run = m.Value;
        var start = 0;
        var end = run.Length;
        while (start < end && run[start] is ' ' or '.' or ',' or ':' or ';' or ')' or '(' && !StartsMath(run, start)) start++;
        while (end > start && run[end - 1] is ' ' or '.' or ',' or ':' or ';' or '(') end--;
        var core = run[start..end];
        if (core.Length == 0 || !MathChar.IsMatch(core) || !BalancedParens(core)) return run;
        return run[..start] + Lri + core + Pdi + run[end..];
    });

    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(text))]
    internal static string? BidiOrNull(string? text) => text == null ? null : Bidi(text);

    // An opening bracket stays inside the isolate only if the math it opens follows it.
    private static bool StartsMath(string run, int i) =>
        run[i] == '(' && i + 1 < run.Length && MathChar.IsMatch(run[(i + 1)..(i + 2)]);

    private static bool BalancedParens(string s) =>
        s.Count(c => c == '(') == s.Count(c => c == ')') && s.Count(c => c == '{') == s.Count(c => c == '}');

    private static bool IsLocalServer(string dataSource)
    {
        var host = dataSource.Replace("tcp:", "", StringComparison.OrdinalIgnoreCase).Split('\\', ',')[0].Trim().ToLowerInvariant();
        return host is "(localdb)" or "." or "(local)" or "localhost" or "127.0.0.1" or "::1"
               || host == Environment.MachineName.ToLowerInvariant();
    }
}
