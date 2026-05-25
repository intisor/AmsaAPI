using AmsaAPI.DTOs;
using AmsaAPI.Tests.Infrastructure;

namespace AmsaAPI.Tests.Endpoints;

public class MemberEndpointsTests : IntegrationTestBase
{
    [Fact]
    public async Task GetAllMembers_WithMembers_ReturnsOkWithMembers()
    {
        // Arrange
        await DbSeedHelper.SeedMembersAsync(DbContext, count: 3);

        // Act
        var response = await Client.GetAsync("/api/minimal/members/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await response.Content.ReadAsAsync<List<MemberDetailResponse>>();
        Assert.NotNull(members);
        Assert.Equal(3, members.Count);
    }

    [Fact]
    public async Task GetAllMembers_WithoutMembers_ReturnsOkWithEmptyList()
    {
        // Act
        var response = await Client.GetAsync("/api/minimal/members/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await response.Content.ReadAsAsync<List<MemberDetailResponse>>();
        Assert.NotNull(members);
        Assert.Empty(members);
    }

    [Fact]
    public async Task GetMemberById_WithValidId_ReturnsOkWithMember()
    {
        // Arrange
        await DbSeedHelper.SeedMembersAsync(DbContext, count: 1);
        var member = DbContext.Members.First();

        // Act
        var response = await Client.GetAsync($"/api/minimal/members/{member.MemberId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<MemberDetailResponse>();
        Assert.NotNull(result);
        Assert.Equal(member.FirstName, result.FirstName);
    }

    [Fact]
    public async Task GetMemberById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/minimal/members/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMemberByMkanId_WithValidMkanId_ReturnsOkWithMember()
    {
        // Arrange
        await DbSeedHelper.SeedMembersAsync(DbContext, count: 1);
        var member = DbContext.Members.First();

        // Act
        var response = await Client.GetAsync($"/api/minimal/members/mkan/{member.MkanId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<MemberDetailResponse>();
        Assert.NotNull(result);
        Assert.Equal(member.MkanId, result.MkanId);
    }

    [Fact]
    public async Task GetMemberByMkanId_WithInvalidMkanId_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/minimal/members/mkan/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMembersByUnit_WithValidUnitId_ReturnsOkWithMembers()
    {
        // Arrange
        await DbSeedHelper.SeedMembersAsync(DbContext, count: 3);
        var unit = DbContext.Units.First();

        // Act
        var response = await Client.GetAsync($"/api/minimal/members/unit/{unit.UnitId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await response.Content.ReadAsAsync<List<MemberDetailResponse>>();
        Assert.NotNull(members);
        Assert.NotEmpty(members);
    }

    [Fact]
    public async Task GetMembersByDepartment_WithValidDepartmentId_ReturnsOkWithMembers()
    {
        // Arrange
        await DbSeedHelper.SeedMembersAsync(DbContext, count: 2);
        var member = DbContext.Members.First();
        var department = await DbContext.Departments.FirstAsync() ?? new Department { DepartmentName = "Test Dept", DepartmentCode = "TD" };
        if (department.DepartmentId == 0)
        {
            DbContext.Departments.Add(department);
            await DbContext.SaveChangesAsync();
        }

        member.Departments?.Add(department);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/minimal/members/department/{department.DepartmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await response.Content.ReadAsAsync<List<MemberDetailResponse>>();
        Assert.NotNull(members);
    }

    [Fact]
    public async Task SearchMembersByName_WithValidName_ReturnsOkWithMembers()
    {
        // Arrange
        await DbSeedHelper.SeedMembersAsync(DbContext, count: 3);

        // Act
        var response = await Client.GetAsync("/api/minimal/members/search/Test1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await response.Content.ReadAsAsync<List<MemberDetailResponse>>();
        Assert.NotNull(members);
    }

    [Fact]
    public async Task CreateMember_WithValidData_ReturnsCreatedAtRoute()
    {
        // Arrange
        await DbSeedHelper.SeedMembersAsync(DbContext, count: 0);
        var unit = DbContext.Units.First();
        var request = new CreateMemberRequest
        {
            FirstName = "John",
            LastName = "Doe",
            MkanId = 9999,
            UnitId = unit.UnitId,
            Gender = "M",
            Email = "john@example.com",
            PhoneNumber = "08000000000"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/minimal/members/", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdMember = await response.Content.ReadAsAsync<MemberDetailResponse>();
        Assert.NotNull(createdMember);
        Assert.Equal("John", createdMember.FirstName);
    }

    [Fact]
    public async Task UpdateMember_WithValidData_ReturnsNoContent()
    {
        // Arrange
        await DbSeedHelper.SeedMembersAsync(DbContext, count: 1);
        var member = DbContext.Members.First();
        var request = new UpdateMemberRequest
        {
            FirstName = "Updated",
            LastName = member.LastName,
            Email = "updated@example.com",
            PhoneNumber = member.PhoneNumber
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/minimal/members/{member.MemberId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await RefreshEntityAsync(member);
        Assert.Equal("Updated", member.FirstName);
    }

    [Fact]
    public async Task DeleteMember_WithValidId_ReturnsNoContent()
    {
        // Arrange
        await DbSeedHelper.SeedMembersAsync(DbContext, count: 1);
        var member = DbContext.Members.First();
        var memberId = member.MemberId;

        // Act
        var response = await Client.DeleteAsync($"/api/minimal/members/{memberId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var exists = await EntityExistsAsync<Member>(q => q.Where(m => m.MemberId == memberId));
        Assert.False(exists);
    }
}
