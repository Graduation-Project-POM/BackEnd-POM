namespace MIO2.Core.Events.Students
{
    public record StudentAcceptedEvent
    (
        int StudentId,
        string Email,
        string FirstName
    );
}
