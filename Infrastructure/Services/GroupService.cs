using Microsoft.EntityFrameworkCore;
using MIO.API.Settings;
using MIO.Core.DTOs;
using MIO.Core.Entities;
using MIO.Core.Interfaces;
using MIO2.Core.DTOs.GroupDTOs;
using MIO2.Core.Entities;
using MIO2.Core.Interfaces;

namespace MIO.Infrastructure.Services;

public class GroupService(AppDbContext db) : IGroupService
{
    public async Task<IEnumerable<GroupResponseDto>> GetAllAsync()
    {
        return await db.Groups
            .Select(g => new GroupResponseDto(g.Id, g.Name, g.Description, g.StartDate, g.TrainerId))
            .ToListAsync();
    }

    public async Task<GroupResponseDto> GetByIdAsync(int id)
    {
        var group = await db.Groups
            .Where(g => g.Id == id)
            .Select(g => new GroupResponseDto(g.Id, g.Name, g.Description, g.StartDate, g.TrainerId))
            .FirstOrDefaultAsync();

        if (group is null)
            throw new AppException($"Group with ID {id} not found", StatusCodes.Status404NotFound);

        return group;
    }

    public async Task<GroupResponseDto> CreateAsync(CreateGroupDto dto)
    {
        bool trainerExists = await db.Trainers.AnyAsync(t => t.Id == dto.TrainerId);
        if (!trainerExists)
            throw new AppException($"Trainer with ID {dto.TrainerId} not found", StatusCodes.Status404NotFound);

        var group = new Group
        {
            Name = dto.Name,
            Description = dto.Description,
            TrainerId = dto.TrainerId
        };

        db.Groups.Add(group);
        await db.SaveChangesAsync();

        return new GroupResponseDto(group.Id, group.Name, group.Description, group.StartDate, group.TrainerId);
    }

    public async Task<bool> UpdateAsync(int id, UpdateGroupDto dto)
    {
        var group = await db.Groups.FindAsync(id);
        if (group is null)
            throw new AppException($"Group with ID {id} not found", StatusCodes.Status404NotFound);

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

        return await db.GroupStudents
            .Where(gs => gs.GroupId == groupId)
            .Select(gs => new GroupStudentDto(
                gs.StudentId,
                gs.Student.User.UserName,
                gs.JoinDate
            ))
            .ToListAsync();
    }

    public async Task<AddStudentsToGroupResponseDto> AddStudentsAsync(int groupId, AddStudentsToGroupDto dto)
    {
        bool groupExists = await db.Groups.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
            throw new AppException($"Group with ID {groupId} not found", StatusCodes.Status404NotFound);

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

        return await db.GroupContests
            .Where(gc => gc.GroupId == groupId)
            .Select(gc => new GroupContestDto(
                gc.ContestId,
                gc.Contest.Title,
                gc.AddDate
            ))
            .ToListAsync();
    }

    public async Task<bool> AddContestAsync(int groupId, int contestId)
    {
        bool groupExists = await db.Groups.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
            throw new AppException($"Group with ID {groupId} not found", StatusCodes.Status404NotFound);

        bool contestExists = await db.Contests.AnyAsync(c => c.Id == contestId);
        if (!contestExists)
            throw new AppException($"Contest with ID {contestId} not found", StatusCodes.Status404NotFound);

        bool alreadyIn = await db.GroupContests
            .AnyAsync(gc => gc.GroupId == groupId && gc.ContestId == contestId);
        if (alreadyIn)
            throw new AppException($"Contest {contestId} is already in group {groupId}", StatusCodes.Status409Conflict);

        db.GroupContests.Add(new GroupContest { GroupId = groupId, ContestId = contestId });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveContestAsync(int groupId, int contestId)
    {
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

        return await db.GroupStudents
            .Where(gs => gs.StudentId == studentId)
            .SelectMany(gs => gs.Group.GroupContests)
            .Select(gc => new GroupContestDto(
                gc.ContestId,
                gc.Contest.Title,
                gc.AddDate
            ))
            .Distinct()
            .ToListAsync();
    }
}