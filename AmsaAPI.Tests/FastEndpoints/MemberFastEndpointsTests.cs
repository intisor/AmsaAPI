using AmsaAPI.DTOs;
using AmsaAPI.Tests.Infrastructure;

namespace AmsaAPI.Tests.FastEndpoints;

public class GetAllMembersEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task GetAllMembers_WithMembers_ReturnsOkWithMembersList()
    {
        // Arrange
        await DbSeedHelper.SeedMembersAsync(DbContext, count: 3);

        // Act
        var response = await Client.GetAsync("/api/members");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await response.Content.ReadFromJsonAsync<List<MemberDetailResponse>>();
        Assert.NotNull(members);
        Assert.Equal(3, members.Count);
    }

    [Fact]
    public async Task GetAllMembers_WithoutMembers_ReturnsOkWithEmptyList()
    {
        // Act
        var response = await Client.GetAsync("/api/members");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await response.Content.ReadFromJsonAsync<List<MemberDetailResponse>>();
        Assert.NotNull(members);
        Assert.Empty(members);
    }
}

public class GetMemberByIdFastEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task GetMemberById_WithValidId_ReturnsOkWithMemberDetails()
    {
        // Arrange
        await DbSeedHelper.SeedMembersAsync(DbContext, count: 1);
        var member = DbContext.Members.First();

        // Act
        var response = await Client.GetAsync($"/api/members/{member.MemberId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MemberDetailResponse>();
        Assert.NotNull(result);
        Assert.Equal(member.FirstName, result.FirstName);
    }

    [Fact]
    public async Task GetMemberById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/members/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
