using Microsoft.EntityFrameworkCore;
using MIO.API.Settings;
using MIO.Core.DTOs.ProblemDTOs;
using MIO.Core.Interfaces;
using MIO.Infrastructure;
using MIO2.Core.Entities;
using MIO2.Core.Interfaces;

namespace MIO2.Infrastructure.Services;

public class ProblemDiscussionService : IProblemDiscussionService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ProblemDiscussionService(AppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    private bool IsModerator => _currentUser.Role is "Admin" or "Trainer";

    private async Task EnsureProblemVisibleAsync(int problemId)
    {
        var problem = await _context.Problems
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ID == problemId);

        var notFound = new AppException(
            $"Problem with ID {problemId} not found",
            StatusCodes.Status404NotFound);

        if (problem == null) throw notFound;

        var role = _currentUser.Role;
        var isAdmin = role == "Admin";

        int? myTrainerId = null;
        if (role == "Trainer")
        {
            myTrainerId = await _context.Trainers
                .Where(t => t.UserId == _currentUser.UserId)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync();
        }

        var ownsProblem = myTrainerId != null && problem.TrainerID == myTrainerId;

        if (!problem.IsBankProblem && !isAdmin && !ownsProblem) throw notFound;
        if (role == "Student" && !problem.IsPublished) throw notFound;
        if (role == "Trainer" && !problem.IsPublished && !ownsProblem) throw notFound;
        if (!problem.IsBankVisible && !isAdmin && !ownsProblem) throw notFound;
    }

    private ProblemCommentDto ToDto(ProblemComment c) => new()
    {
        Id = c.Id,
        ProblemId = c.ProblemId,
        ParentCommentId = c.ParentCommentId,
        AuthorId = c.UserId,
        AuthorName = c.User == null
            ? string.Empty
            : $"{c.User.FirstName} {c.User.LastName}".Trim(),
        AuthorRole = c.User == null ? string.Empty : c.User.RoleID.ToString(),
        AuthorImageUrl = c.User?.ProfileImageUrl,
        Body = c.Body,
        PhotoUrl = c.PhotoFileKey,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        IsMine = c.UserId == _currentUser.UserId,
        CanEdit = c.UserId == _currentUser.UserId,
        CanDelete = c.UserId == _currentUser.UserId || IsModerator,
    };

    public async Task<IEnumerable<ProblemCommentDto>> GetDiscussion(int problemId)
    {
        await EnsureProblemVisibleAsync(problemId);

        var comments = await _context.ProblemComments
            .AsNoTracking()
            .Where(c => c.ProblemId == problemId)
            .Include(c => c.User)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        var roots = comments
            .Where(c => c.ParentCommentId == null)
            .Select(ToDto)
            .ToList();

        var repliesByParent = comments
            .Where(c => c.ParentCommentId != null)
            .GroupBy(c => c.ParentCommentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(ToDto).ToList());

        foreach (var root in roots)
        {
            if (repliesByParent.TryGetValue(root.Id, out var replies))
                root.Replies = replies;
        }

        return roots;
    }

    public async Task<ProblemCommentDto> AddComment(int problemId, CreateProblemCommentDto dto)
    {
        await EnsureProblemVisibleAsync(problemId);

        var body = dto.Body?.Trim();
        if (string.IsNullOrWhiteSpace(body) && string.IsNullOrWhiteSpace(dto.PhotoFileKey))
            throw new AppException("A message or an image is required", StatusCodes.Status400BadRequest);

        int? parentId = null;
        if (dto.ParentCommentId is int requestedParent)
        {
            var parent = await _context.ProblemComments
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == requestedParent && c.ProblemId == problemId);

            if (parent == null)
                throw new AppException(
                    $"Comment {requestedParent} not found on this problem",
                    StatusCodes.Status404NotFound);

            parentId = parent.ParentCommentId ?? parent.Id;
        }

        var comment = new ProblemComment
        {
            ProblemId = problemId,
            UserId = _currentUser.UserId,
            ParentCommentId = parentId,
            Body = body,
            PhotoFileKey = dto.PhotoFileKey,
            CreatedAt = DateTime.UtcNow,
        };

        _context.ProblemComments.Add(comment);
        await _context.SaveChangesAsync();

        await _context.Entry(comment).Reference(c => c.User).LoadAsync();
        return ToDto(comment);
    }

    public async Task<ProblemCommentDto> UpdateComment(int commentId, UpdateProblemCommentDto dto)
    {
        var comment = await _context.ProblemComments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null)
            throw new AppException($"Comment {commentId} not found", StatusCodes.Status404NotFound);

        if (comment.UserId != _currentUser.UserId)
            throw new AppException("You can only edit your own messages", StatusCodes.Status403Forbidden);

        var body = dto.Body?.Trim();
        if (string.IsNullOrWhiteSpace(body) && string.IsNullOrWhiteSpace(dto.PhotoFileKey))
            throw new AppException("A message or an image is required", StatusCodes.Status400BadRequest);

        comment.Body = body;
        comment.PhotoFileKey = dto.PhotoFileKey;
        comment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ToDto(comment);
    }

    public async Task DeleteComment(int commentId)
    {
        var comment = await _context.ProblemComments
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null)
            throw new AppException($"Comment {commentId} not found", StatusCodes.Status404NotFound);

        if (comment.UserId != _currentUser.UserId && !IsModerator)
            throw new AppException("You can only delete your own messages", StatusCodes.Status403Forbidden);

        if (comment.ParentCommentId == null)
        {
            var replies = await _context.ProblemComments
                .Where(c => c.ParentCommentId == comment.Id)
                .ToListAsync();

            if (replies.Count > 0)
                _context.ProblemComments.RemoveRange(replies);
        }

        _context.ProblemComments.Remove(comment);
        await _context.SaveChangesAsync();
    }
}
