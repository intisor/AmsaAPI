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
        var members = await response.Content.ReadAsAsync<List<MemberSummaryDto>>();
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
        var members = await response.Content.ReadAsAsync<List<MemberSummaryDto>>();
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
        var result = await response.Content.ReadAsAsync<MemberDetailDto>();
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

public class CreateMemberFastEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateMember_WithValidData_ReturnsCreatedStatus()
    {
        // Arrange
        await DbSeedHelper.SeedMembersAsync(DbContext, count: 0);
        var unit = DbContext.Units.First();
        var request = new CreateMemberRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            MkanId = 8888,
            UnitId = unit.UnitId,
            Gender = "F",
            Email = "jane@example.com",
            PhoneNumber = "08000000001"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/members", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdMember = await response.Content.ReadAsAsync<MemberDetailDto>();
        Assert.NotNull(createdMember);
        Assert.Equal("Jane", createdMember.FirstName);
    }

    [Fact]
    public async Task CreateMember_WithDuplicateMkanId_ReturnsBadRequest()
    {
        // Arrange
        await DbSeedHelper.SeedMembersAsync(DbContext, count: 1);
        var existingMember = DbContext.Members.First();
        var request = new CreateMemberRequest
        {
            FirstName = "Duplicate",
            LastName = "Test",
            MkanId = existingMember.MkanId,
            UnitId = existingMember.UnitId,
            Gender = "M",
            Email = "dup@example.com",
            PhoneNumber = "08000000002"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/members", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
