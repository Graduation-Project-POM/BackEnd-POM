using Microsoft.EntityFrameworkCore;
using MIO.API.Settings;
using MIO.Core.DTOs;
using MIO.Core.Entities;
using MIO.Core.Interfaces;
using MIO2.Core.DTOs.GroupDTOs;
using MIO2.Core.Entities;
using MIO2.Core.Interfaces;

namespace MIO.Infrastructure.Services;

public class GroupService(AppDbContext db, ICurrentUserService currentUser) : IGroupService
{
    public async Task<IEnumerable<GroupResponseDto>> GetAllAsync()
    {
        var isAdmin = currentUser.Role == "Admin";

        var trainerId = await db.Trainers
            .Where(t => t.UserId == currentUser.UserId)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

        var query = db.Groups.AsQueryable();

        if (!isAdmin)
        {
            // A trainer may only see the groups they own. A user with the Trainer
            // role but no Trainer profile owns nothing — return an empty list
            // rather than leaking unowned (TrainerId == null) groups.
            if (trainerId == null)
                return [];

            query = query.Where(g => g.TrainerId == trainerId);
        }

        return await query
            .Select(g => new GroupResponseDto(
                g.Id, g.Name, g.Description, g.StartDate, g.TrainerId ?? 0, g.GroupStudents.Count,
                g.Trainer != null ? ((g.Trainer.User.FirstName + " " + g.Trainer.User.LastName).Trim()) : null,
                g.Trainer != null ? g.Trainer.User.Email : null,
                g.Trainer != null ? g.Trainer.Specialization : null))
            .ToListAsync();
    }

    public async Task<GroupResponseDto> GetByIdAsync(int id)
    {
        var group = await db.Groups
            .Where(g => g.Id == id)
            .Select(g => new GroupResponseDto(
                g.Id, g.Name, g.Description, g.StartDate, g.TrainerId ?? 0, g.GroupStudents.Count,
                g.Trainer != null ? ((g.Trainer.User.FirstName + " " + g.Trainer.User.LastName).Trim()) : null,
                g.Trainer != null ? g.Trainer.User.Email : null,
                g.Trainer != null ? g.Trainer.Specialization : null))
            .FirstOrDefaultAsync();

        if (group is null)
            throw new AppException($"Group with ID {id} not found", StatusCodes.Status404NotFound);

        await EnsureCanViewGroupAsync(id);
        return group;
    }

    public async Task<GroupResponseDto> CreateAsync(CreateGroupDto dto)
    {
        int? trainerId = null;
        if (currentUser.Role == "Trainer")
        {
            trainerId = await CurrentTrainerIdAsync();
            if (trainerId == null)
                throw new AppException("You must be a Trainer to create a group", StatusCodes.Status403Forbidden);
        }
        else if (dto.TrainerId > 0)
        {
            bool trainerExists = await db.Trainers.AnyAsync(t => t.Id == dto.TrainerId);
            if (!trainerExists)
                throw new AppException($"Trainer with ID {dto.TrainerId} not found", StatusCodes.Status404NotFound);
            trainerId = dto.TrainerId;
        }

        var group = new Group
        {
            Name = dto.Name,
            Description = dto.Description,
            TrainerId = trainerId
        };

        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var trainer = group.TrainerId is null
            ? null
            : await db.Trainers
                .Where(tr => tr.Id == group.TrainerId)
                .Select(tr => new
                {
                    Name = (tr.User.FirstName + " " + tr.User.LastName).Trim(),
                    tr.User.Email,
                    tr.Specialization,
                })
                .FirstOrDefaultAsync();

        return new GroupResponseDto(
            group.Id, group.Name, group.Description, group.StartDate, group.TrainerId ?? 0, 0,
            trainer?.Name, trainer?.Email, trainer?.Specialization);
    }

    public async Task<bool> UpdateAsync(int id, UpdateGroupDto dto)
    {
        var group = await db.Groups.FindAsync(id);
        if (group is null)
            throw new AppException($"Group with ID {id} not found", StatusCodes.Status404NotFound);

        await EnsureCanManageGroupAsync(id);

        group.Name = dto.Name;
        group.Description = dto.Description;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var group = await db.Groups.FindAsync(id);
        if (group is null)
            throw new AppException($"Group with ID {id} not found", StatusCodes.Status404NotFound);

        await EnsureCanManageGroupAsync(id);

        // Deleting a group also deletes the contests assigned to it. Removing the
        // Contest rows cascades to their participants (ContestStudents), their
        // problem links (ContestProblem) and the GroupContest links, so no
        // orphaned contest data is left behind. Students' solved history
        // (ProblemStudents) is keyed to Problem, not Contest, so it is preserved.
        var contests = await db.Contests
            .Where(c => db.GroupContests.Any(gc => gc.GroupId == id && gc.ContestId == c.Id))
            .ToListAsync();

        if (contests.Count > 0)
            db.Contests.RemoveRange(contests);

        db.Groups.Remove(group);
        await db.SaveChangesAsync();
        return true;
    }

    // ── Students ──────────────────────────────────────────────

    public async Task<IEnumerable<GroupStudentDto>> GetStudentsAsync(int groupId)
    {
        bool groupExists = await db.Groups.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
            throw new AppException($"Group with ID {groupId} not found", StatusCodes.Status404NotFound);

        await EnsureCanViewGroupAsync(groupId);

        // Problems that belong to this group's contests — the pool a member's
        // "solved" count is measured against.
        var groupProblemIds = await db.GroupContests
            .Where(gc => gc.GroupId == groupId)
            .SelectMany(gc => gc.Contest.Problems.Select(p => p.ID))
            .Distinct()
            .ToListAsync();

        return await db.GroupStudents
            .Where(gs => gs.GroupId == groupId)
            .Select(gs => new GroupStudentDto(
                gs.StudentId,
                gs.Student.User.UserName,
                gs.JoinDate,
                // Real count: problems in this group's contests the student
                // has submitted a solution for.
                db.ProblemStudents.Count(ps =>
                    ps.StudentId == gs.StudentId &&
                    groupProblemIds.Contains(ps.ProblemId))
            ))
            .ToListAsync();
    }

    public async Task<AddStudentsToGroupResponseDto> AddStudentsAsync(int groupId, AddStudentsToGroupDto dto)
    {
        bool groupExists = await db.Groups.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
            throw new AppException($"Group with ID {groupId} not found", StatusCodes.Status404NotFound);

        await EnsureCanManageGroupAsync(groupId);

        var existingStudentIds = await db.GroupStudents
            .Where(gs => gs.GroupId == groupId)
            .Select(gs => gs.StudentId)
            .ToListAsync();

        var alreadyInGroup = dto.StudentIds
            .Intersect(existingStudentIds)
            .ToList();

        var newStudentIds = dto.StudentIds
            .Except(existingStudentIds)
            .ToList();

        var students = await db.Students
            .Where(s => newStudentIds.Contains(s.Id))
            .ToListAsync();

        var missingIds = newStudentIds
            .Except(students.Select(s => s.Id))
            .ToList();

        var groupStudents = students.Select(s => new GroupStudent
        {
            GroupId = groupId,
            StudentId = s.Id
        });

        db.GroupStudents.AddRange(groupStudents);
        await db.SaveChangesAsync();

        return new AddStudentsToGroupResponseDto(
            Message: $"{students.Count} students added successfully",
            AlreadyInGroup: alreadyInGroup,
            MissingIds: missingIds
        );
    }

    public async Task<bool> RemoveStudentAsync(int groupId, int studentId)
    {
        await EnsureCanManageGroupAsync(groupId);

        var entry = await db.GroupStudents
            .FirstOrDefaultAsync(gs => gs.GroupId == groupId && gs.StudentId == studentId);

        if (entry is null)
            throw new AppException($"Student {studentId} is not in group {groupId}", StatusCodes.Status404NotFound);

        db.GroupStudents.Remove(entry);
        await db.SaveChangesAsync();
        return true;
    }

    // ── Contests ──────────────────────────────────────────────

    public async Task<IEnumerable<GroupContestDto>> GetContestsAsync(int groupId)
    {
        bool groupExists = await db.Groups.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
            throw new AppException($"Group with ID {groupId} not found", StatusCodes.Status404NotFound);

        await EnsureCanViewGroupAsync(groupId);

        var contests = await db.GroupContests
            .Where(gc => gc.GroupId == groupId)
            .Select(gc => new GroupContestDto(
                gc.ContestId,
                gc.Contest.Title,
                gc.AddDate,
                gc.Contest.StartAt,
                gc.Contest.EndAt,
                0
            ))
            .ToListAsync();

        return await WithParticipantCounts(contests);
    }

    public async Task<bool> AddContestAsync(int groupId, int contestId)
    {
        bool groupExists = await db.Groups.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
            throw new AppException($"Group with ID {groupId} not found", StatusCodes.Status404NotFound);

        await EnsureCanManageGroupAsync(groupId);

        bool contestExists = await db.Contests.AnyAsync(c => c.Id == contestId);
        if (!contestExists)
            throw new AppException($"Contest with ID {contestId} not found", StatusCodes.Status404NotFound);

        bool alreadyIn = await db.GroupContests
            .AnyAsync(gc => gc.GroupId == groupId && gc.ContestId == contestId);
        if (alreadyIn)
            throw new AppException($"Contest {contestId} is already in group {groupId}", StatusCodes.Status409Conflict);

        bool inAnotherGroup = await db.GroupContests
            .AnyAsync(gc => gc.ContestId == contestId && gc.GroupId != groupId);
        if (inAnotherGroup)
            throw new AppException($"Contest {contestId} already belongs to another group", StatusCodes.Status409Conflict);

        db.GroupContests.Add(new GroupContest { GroupId = groupId, ContestId = contestId });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveContestAsync(int groupId, int contestId)
    {
        await EnsureCanManageGroupAsync(groupId);

        var entry = await db.GroupContests
            .FirstOrDefaultAsync(gc => gc.GroupId == groupId && gc.ContestId == contestId);

        if (entry is null)
            throw new AppException($"Contest {contestId} is not in group {groupId}", StatusCodes.Status404NotFound);

        db.GroupContests.Remove(entry);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<GroupContestDto>> GetContestsForStudentAsync(int studentId)
    {
        bool studentExists = await db.Students.AnyAsync(s => s.Id == studentId);
        if (!studentExists)
            throw new AppException($"Student with ID {studentId} not found", StatusCodes.Status404NotFound);

        var contests = await db.GroupStudents
            .Where(gs => gs.StudentId == studentId)
            .SelectMany(gs => gs.Group.GroupContests)
            .Select(gc => new GroupContestDto(
                gc.ContestId,
                gc.Contest.Title,
                gc.AddDate,
                gc.Contest.StartAt,
                gc.Contest.EndAt,
                0
            ))
            .Distinct()
            .ToListAsync();

        return await WithParticipantCounts(contests);
    }

    private async Task<List<GroupContestDto>> WithParticipantCounts(List<GroupContestDto> contests)
    {
        var counts = await db.CountByContestAsync(contests.Select(c => c.ContestId).Distinct().ToList());
        return contests
            .Select(c => c with { ParticipantCount = counts.GetValueOrDefault(c.ContestId) })
            .ToList();
    }

    public async Task<IEnumerable<GroupResponseDto>> GetGroupsForStudentByUserIdAsync(int userId)
    {
        var studentId = await db.Students
            .Where(s => s.UserId == userId)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();

        // A user with no student profile simply belongs to no groups.
        if (studentId is null)
            return [];

        return await db.GroupStudents
            .Where(gs => gs.StudentId == studentId)
            .Select(gs => gs.Group)
            .Select(g => new GroupResponseDto(
                g.Id, g.Name, g.Description, g.StartDate, g.TrainerId ?? 0, g.GroupStudents.Count,
                g.Trainer != null ? ((g.Trainer.User.FirstName + " " + g.Trainer.User.LastName).Trim()) : null,
                g.Trainer != null ? g.Trainer.User.Email : null,
                g.Trainer != null ? g.Trainer.Specialization : null))
            .ToListAsync();
    }

    private Task<int?> CurrentTrainerIdAsync() =>
        db.Trainers
            .Where(t => t.UserId == currentUser.UserId)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

    private async Task EnsureCanManageGroupAsync(int groupId)
    {
        if (currentUser.Role == "Admin") return;

        var groupTrainerId = await db.Groups
            .Where(g => g.Id == groupId)
            .Select(g => g.TrainerId)
            .FirstOrDefaultAsync();

        var myTrainerId = await CurrentTrainerIdAsync();

        if (myTrainerId == null || groupTrainerId == null || groupTrainerId != myTrainerId)
            throw new AppException("You are not authorized to manage this group", StatusCodes.Status403Forbidden);
    }

    private async Task EnsureCanViewGroupAsync(int groupId)
    {
        if (currentUser.Role == "Admin") return;

        if (currentUser.Role == "Trainer")
        {
            var groupTrainerId = await db.Groups
                .Where(g => g.Id == groupId)
                .Select(g => g.TrainerId)
                .FirstOrDefaultAsync();
            var myTrainerId = await CurrentTrainerIdAsync();

            if (myTrainerId != null && groupTrainerId == myTrainerId) return;
            throw new AppException("You are not authorized to view this group", StatusCodes.Status403Forbidden);
        }

        var isMember = await db.GroupStudents
            .AnyAsync(gs => gs.GroupId == groupId && gs.Student.UserId == currentUser.UserId);
        if (!isMember)
            throw new AppException("You are not authorized to view this group", StatusCodes.Status403Forbidden);
    }
}