using GraphQL.Integration.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace GraphQL.Integration.Tests.WebsocketTests
{
    public class UnityNewtonsoft : UnityBase, IClassFixture<NewtonsoftIntegrationServerTestFixture>
    {
        public UnityNewtonsoft(ITestOutputHelper output, NewtonsoftIntegrationServerTestFixture fixture) : base(output, fixture)
        {
        }
    }
}
