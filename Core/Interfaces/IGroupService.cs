using MIO2.Core.DTOs.GroupDTOs;

namespace MIO2.Core.Interfaces;

public interface IGroupService
{
    Task<IEnumerable<GroupResponseDto>> GetAllAsync();
    Task<GroupResponseDto?> GetByIdAsync(int id);

    Task<GroupResponseDto> CreateAsync(CreateGroupDto dto);
    Task<bool> UpdateAsync(int id, UpdateGroupDto dto);
    Task<bool> DeleteAsync(int id);

    // Students
    Task<AddStudentsToGroupResponseDto> AddStudentsAsync(int groupId, AddStudentsToGroupDto dto);
    Task<bool> RemoveStudentAsync(int groupId, int studentId);
    Task<IEnumerable<GroupStudentDto>> GetStudentsAsync(int groupId);

    // Contests
    Task<bool> AddContestAsync(int groupId, int contestId);
    Task<bool> RemoveContestAsync(int groupId, int contestId);
    Task<IEnumerable<GroupContestDto>> GetContestsAsync(int groupId);

    // Get all contests accessible to a student (via their groups)
    Task<IEnumerable<GroupContestDto>> GetContestsForStudentAsync(int studentId);
}