using MIO2.Core.Events.Students;

namespace MIO2.Core.Events.EventBus
{
    public class studentEvents
    {
        public static event Func<StudentAcceptedEvent, Task>? Accepted;

        public static async Task RaiseAccepted(StudentAcceptedEvent e)
        {

            if (Accepted == null) return;

            foreach (var handler in Accepted.GetInvocationList())
            {
                await ((Func<StudentAcceptedEvent, Task>)handler)(e);
            }


        }
    }
}
