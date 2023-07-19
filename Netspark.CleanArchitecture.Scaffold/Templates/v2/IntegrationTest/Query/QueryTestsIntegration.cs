namespace NamespacePlaceholder
{
    using System.Net;
    using System.Threading.Tasks;
    using QueryNsPlaceholder;
    using QueriesNsPlaceholder;
    using WebNsPlaceholder;
    using WebNsPlaceholder.IntegrationTests.Common;
    using Xunit;
    using Xunit.Abstractions;

    public class FixturePlaceholder : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly CustomWebApplicationFactory<Startup> _factory;
        private readonly ITestOutputHelper _logger;

        public FixturePlaceholder(CustomWebApplicationFactory<Startup> factory, ITestOutputHelper logger)
        {
            _factory = factory;
            _logger = logger;
        }

        [Fact]
        public async Task QueryPlaceholder_ReturnsSuccessResult()
        {
            var client = await _factory.GetAuthenticatedClientAsync();

            var response = await client.GetAsync("UrlPlaceholder");
            var list = await Utilities.GetResponseContent<VmPlaceholder>(response);

            response.EnsureSuccessStatusCode();
            Assert.NotNull(list);
        }

        [Fact]
        public async Task QueryPlaceholder_ReturnsUnauthorizedResult()
        {
            var client = _factory.GetAnonymousClient();

            var response = await client.GetAsync("UrlPlaceholder");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
