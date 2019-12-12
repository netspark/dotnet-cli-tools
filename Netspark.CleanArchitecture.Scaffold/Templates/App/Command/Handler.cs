using System.Threading;
using System.Threading.Tasks;
using ApplicationNsPlaceholder.Common.Exceptions;
using ApplicationNsPlaceholder.Common.Interfaces;
using DomainNsPlaceholder.Entities;
using MediatR;

namespace NamespacePlaceholder
{
    public class HandlerPlaceholder : IRequestHandler<CommandPlaceholder, Unit>
    {
        private readonly DbContextPlaceholder _context;
        private readonly IMediator _mediator;

        public HandlerPlaceholder(DbContextPlaceholder context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }
        
        public async Task<Unit> Handle(CommandPlaceholder request, CancellationToken cancellationToken)
        {
            // TODO: implement command code here

            await _context.SaveChangesAsync(cancellationToken);

            await _mediator.Publish(new EventPlaceholder 
            {
                // TODO: fill event properties here
            }, cancellationToken);
            
            return Unit.Value;
        }
    }
}