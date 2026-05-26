using AmsaAPI.DTOs;
using AmsaAPI.Tests.Infrastructure;

namespace AmsaAPI.Tests.Endpoints;

public class DepartmentEndpointsTests : IntegrationTestBase
{
    [Fact]
    public async Task GetAllDepartments_WithDepartments_ReturnsOkWithDepartments()
    {
        // Arrange
        await DbSeedHelper.SeedDepartmentsAsync(DbContext, count: 3);

        // Act
        var response = await Client.GetAsync("/api/minimal/departments/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var departments = await response.Content.ReadFromJsonAsync<List<DepartmentSummaryDto>>();
        Assert.NotNull(departments);
        Assert.Equal(3, departments.Count);
    }

    [Fact]
    public async Task GetAllDepartments_WithoutDepartments_ReturnsOkWithEmptyList()
    {
        // Act
        var response = await Client.GetAsync("/api/minimal/departments/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var departments = await response.Content.ReadFromJsonAsync<List<DepartmentSummaryDto>>();
        Assert.NotNull(departments);
        Assert.Empty(departments);
    }

    [Fact]
    public async Task GetDepartmentById_WithValidId_ReturnsOkWithDepartment()
    {
        // Arrange
        await DbSeedHelper.SeedDepartmentsAsync(DbContext, count: 1);
        var department = DbContext.Departments.First();

        // Act
        var response = await Client.GetAsync($"/api/minimal/departments/{department.DepartmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DepartmentDetailResponse>();
        Assert.NotNull(result);
        Assert.Equal(department.DepartmentName, result.DepartmentName);
    }

    [Fact]
    public async Task GetDepartmentById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/minimal/departments/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
