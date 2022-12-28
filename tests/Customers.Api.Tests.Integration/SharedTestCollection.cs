using Xunit;

namespace Customers.Api.Tests.Integration;

[CollectionDefinition("Test collection")]
public class SharedTestCollection : ICollectionFixture<CustomerApiFactory>
{
    
}
