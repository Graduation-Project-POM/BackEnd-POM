namespace MIO.Core.Interfaces
{
    public interface ICurrentUserService
    {

        int UserId { get; }
        string? UserName { get; }
        string? Role { get; }
    }

}
