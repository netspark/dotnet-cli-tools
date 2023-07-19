namespace NamespacePlaceholder
{
    using System.Threading;
    using System.Threading.Tasks;
    using ApplicationNsPlaceholder.Common.Exceptions;
    using CommandNsPlaceholder;
    using ApplicationNsPlaceholder.UnitTests.Common;
    using Xunit;
    using Moq;
    using MediatR;

    public class FixturePlaceholder : CommandTestBase
    {
        private readonly HandlerPlaceholder _sut;
        private readonly Mock<IMediator> _mediator;

        public FixturePlaceholder()
            : base()
        {

            _mediator = new Mock<IMediator>();
            _sut = new HandlerPlaceholder(_context, _mediator.Object);
        }

        [Fact]
        public async Task Handle_When_Then()
        {
            var command = new CommandPlaceholder 
            { 
                // TODO: fill command properties here
            };

            // TODO: replace with actual assertions here
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.Handle(command, CancellationToken.None));
        }

    }
}
