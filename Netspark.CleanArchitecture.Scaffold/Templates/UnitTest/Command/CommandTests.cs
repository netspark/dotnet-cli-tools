using System.Threading;
using System.Threading.Tasks;
using ApplicationNsPlaceholder.Common.Exceptions;
using CommandNsPlaceholder;
using ApplicationNsPlaceholder.UnitTests.Common;
using Xunit;

namespace NamespacePlaceholder
{
    public class FixturePlaceholder : CommandTestBase
    {
        private readonly HandlerPlaceholder _sut;

        public FixturePlaceholder()
            : base()
        {
            _sut = new HandlerPlaceholder(_context);
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
