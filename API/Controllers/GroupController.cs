using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MIO.Core.DTOs;
using MIO.Core.Interfaces;
using MIO2.Core.DTOs.GroupDTOs;
using MIO2.Core.Interfaces;

namespace MIO.API.Controllers;

[ApiController]
[Route("[Controller]")]
[Authorize]
public class GroupController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly ICurrentUserService _currentUser;

    public GroupController(IGroupService groupService, ICurrentUserService currentUser)
    {
        _groupService = groupService;
        _currentUser = currentUser;
    }

    // GET /group
    [HttpGet]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult<IEnumerable<GroupResponseDto>>> GetAll()
    {
        var groups = await _groupService.GetAllAsync();
        return Ok(groups);
    }

    // GET /group/my — groups the logged-in student is a member of.
    [HttpGet("my")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<IEnumerable<GroupResponseDto>>> GetMyGroups()
    {
        var groups = await _groupService.GetGroupsForStudentByUserIdAsync(_currentUser.UserId);
        return Ok(groups);
    }

    // GET /group/5
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult<GroupResponseDto>> GetById(int id)
    {
        var group = await _groupService.GetByIdAsync(id);
        return Ok(group);
    }

    // POST /group
    [HttpPost]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult> Create(CreateGroupDto dto)
    {
        var created = await _groupService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, new
        {
            message = "Group created successfully",
            groupId = created.Id
        });
    }

    // PUT /group/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult> Update(int id, UpdateGroupDto dto)
    {
        await _groupService.UpdateAsync(id, dto);
        return Ok(new { message = "Group updated successfully" });
    }

    // DELETE /group/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult> Delete(int id)
    {
        await _groupService.DeleteAsync(id);
        return Ok(new { message = "Group deleted successfully" });
    }

    // ── Students ──────────────────────────────────────────────

    // GET /group/5/students — members of the group may view its roster.
    [HttpGet("{id}/students")]
    [Authorize(Policy = "GroupMemberOrAdminOrTrainer")]
    public async Task<ActionResult<IEnumerable<GroupStudentDto>>> GetStudents(int id)
    {
        var students = await _groupService.GetStudentsAsync(id);
        return Ok(students);
    }

    // POST /group/5/students
    [HttpPost("{id}/students")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult> AddStudents(int id, AddStudentsToGroupDto dto)
    {
        var result = await _groupService.AddStudentsAsync(id, dto);
        return Ok(result);
    }

    
    // DELETE /group/5/students/3
    [HttpDelete("{id}/students/{studentId}")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult> RemoveStudent(int id, int studentId)
    {
        await _groupService.RemoveStudentAsync(id, studentId);
        return Ok(new { message = "Student removed from group successfully" });
    }

    // ── Contests ─────────────────────────────────────────────

    // GET /group/5/contests
    [HttpGet("{id}/contests")]
    [Authorize(Policy = "GroupMemberOrAdminOrTrainer")]
    public async Task<ActionResult<IEnumerable<GroupContestDto>>> GetContests(int id)
    {
        var contests = await _groupService.GetContestsAsync(id);
        return Ok(contests);
    }

    
    // POST /group/5/contests
    [HttpPost("{id}/contests")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult> AddContest(int id, AddContestToGroupDto dto)
    {
        await _groupService.AddContestAsync(id, dto.ContestId);
        return Ok(new { message = "Contest added to group successfully" });
    }

    // DELETE /group/5/contests/2
    [HttpDelete("{id}/contests/{contestId}")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult> RemoveContest(int id, int contestId)
    {
        await _groupService.RemoveContestAsync(id, contestId);
        return Ok(new { message = "Contest removed from group successfully" });
    }

    // GET /group/student/3/contests
    [HttpGet("student/{studentId}/contests")]
    [Authorize(Policy = "GroupMemberOrAdminOrTrainer")]
    public async Task<ActionResult<IEnumerable<GroupContestDto>>> GetContestsForStudent(int studentId)
    {
        var contests = await _groupService.GetContestsForStudentAsync(studentId);
        return Ok(contests);
    }
}