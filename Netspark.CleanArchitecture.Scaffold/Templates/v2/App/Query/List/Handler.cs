namespace NamespacePlaceholder;

using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class HandlerPlaceholder : IRequestHandler<QueryPlaceholder, VmPlaceholder>
{
    public HandlerPlaceholder()
    {
    }

    public async Task<VmPlaceholder> Handle(QueryPlaceholder request, CancellationToken cancellationToken)
    {
        var items = new List<DtoPlaceholder>();

        var vm = new VmPlaceholder
        {
            Items = items
        };

        return vm;
    }
}
