using MIO.Core.Interfaces;
using MIO2.Core.Events.Students;

namespace MIO2.Infrastructure.Events
{
    public class StudentAcceptedEmailHandler
    {
        private readonly IEmailService _email;

        public StudentAcceptedEmailHandler(IEmailService email)
        {
            _email = email;
        }

        public async Task Handle(StudentAcceptedEvent e)
        {
            var subject = _email.AcceptedInMIOSubject();
            var body = _email.AcceptedInMIOBody(e.FirstName);

            await _email.SendEmailAsync(e.Email,subject, body);
        }
    }
}
