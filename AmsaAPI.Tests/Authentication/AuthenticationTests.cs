using AmsaAPI.Tests.Infrastructure;

namespace AmsaAPI.Tests.Authentication;

public class AuthenticationTests : IntegrationTestBase
{
    [Fact]
    public async Task ValidToken_WithAuthenticatedEndpoint_AllowsAccess()
    {
        // Arrange
        var token = TokenTestHelper.GenerateTestToken("ReportingApp");
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/units/1");

        // Assert
        Assert.NotEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InvalidToken_WithAuthenticatedEndpoint_ReturnsForbidden()
    {
        // Arrange
        var invalidToken = "invalid.token.here";
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", invalidToken);

        // Act
        var response = await Client.GetAsync("/api/units/1");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredToken_WithAuthenticatedEndpoint_ReturnsForbidden()
    {
        // Arrange
        var expiredToken = TokenTestHelper.GenerateExpiredToken("ReportingApp");
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await Client.GetAsync("/api/units/1");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NoToken_WithAuthenticatedEndpoint_ReturnsForbidden()
    {
        // Act
        var response = await Client.GetAsync("/api/units/1");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidToken_WithWrongAudience_ReturnsForbidden()
    {
        // Arrange
        var token = TokenTestHelper.GenerateTestToken("WrongApp");
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/units/1");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public class EndpointAuthorizationTests : IntegrationTestBase
{
    [Fact]
    public async Task AllowAnonymousEndpoint_WithoutToken_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/members");

        // Assert - If endpoint is marked AllowAnonymous, it should work
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_AllowsAccess()
    {
        // Arrange
        var token = TokenTestHelper.GenerateTestToken("ReportingApp");
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/units/1");

        // Assert
        Assert.NotEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
