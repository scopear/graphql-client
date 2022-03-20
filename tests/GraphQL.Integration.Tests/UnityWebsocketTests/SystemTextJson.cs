using GraphQL.Integration.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace GraphQL.Integration.Tests.WebsocketTests
{
    public class UnitySystemTextJson : UnityBase, IClassFixture<SystemTextJsonIntegrationServerTestFixture>
    {
        public UnitySystemTextJson(ITestOutputHelper output, SystemTextJsonIntegrationServerTestFixture fixture) : base(output, fixture)
        {
        }
    }
}
