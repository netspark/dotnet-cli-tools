namespace NamespacePlaceholder;

using MediatR;
using System.Threading;
using System.Threading.Tasks;

public class HandlerPlaceholder : IRequestHandler<QueryPlaceholder, VmPlaceholder>
{
    public HandlerPlaceholder()
    {
    }

    public async Task<VmPlaceholder> Handle(QueryPlaceholder request, CancellationToken cancellationToken)
    {
        // TODO: implement query code here
        
        return new VmPlaceholder();
    }
}
