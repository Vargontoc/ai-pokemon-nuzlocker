using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

[CollectionDefinition("WebApp collection")]
public class WebAppCollectionFixture : ICollectionFixture<WebApplicationFactory<Program>>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
