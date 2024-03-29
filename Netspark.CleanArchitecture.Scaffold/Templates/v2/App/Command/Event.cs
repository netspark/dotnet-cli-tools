namespace NamespacePlaceholder;

using MediatR;
using System.Threading;
using System.Threading.Tasks;

public class EventPlaceholder : INotification
{
    // TODO: Add event properties here

    public class HandlerPlaceholder : INotificationHandler<EventPlaceholder>
    {
        private readonly INotificationService _notification;

        public HandlerPlaceholder(INotificationService notification)
        {
            _notification = notification;
        }

        public async Task Handle(EventPlaceholder notification, CancellationToken cancellationToken)
        {
            //await _notification.SendAsync(new MessageDto());
        }
    }
}