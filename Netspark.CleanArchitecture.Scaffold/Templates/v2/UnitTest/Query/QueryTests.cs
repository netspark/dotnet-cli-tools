namespace NamespacePlaceholder
{
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using QueryNsPlaceholder;
    using ApplicationNsPlaceholder.UnitTests.Common;
    using PersistenceNsPlaceholder;
    using Shouldly;
    using Xunit;

    [Collection("QueryCollection")]
    public class FixturePlaceholder
    { 
        private readonly DbContextPlaceholder _context;
        private readonly IMapper _mapper;

        public FixturePlaceholder(QueryTestFixture fixture)
        {
            _context = fixture.Context;
            _mapper = fixture.Mapper;
        }    

        [Fact]
        public async Task QueryPlaceholder()
        {
            var sut = new HandlerPlaceholder(_context, _mapper);

            var result = await sut.Handle(new QueryPlaceholderQuery 
            { 
                // TODO: fill query properties here 
            }, CancellationToken.None);

            result.ShouldBeOfType<VmPlaceholder>();
        }
    }
}
