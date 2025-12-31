namespace MIO.Core.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
        string AcceptedInMIOSubject();
        string AcceptedInMIOBody(string StudentName);
    }
}
