namespace NamespacePlaceholder;

using MediatR;
using System.Threading;
using System.Threading.Tasks;

public class HandlerPlaceholder : IRequestHandler<CommandPlaceholder, Unit>
{
    public HandlerPlaceholder()
    {
    }
    
    public async Task<Unit> Handle(CommandPlaceholder request, CancellationToken cancellationToken)
    {
        // TODO: implement command code here
       
        return Unit.Value;
    }
}