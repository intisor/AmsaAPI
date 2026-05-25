using AmsaAPI.DTOs;
using AmsaAPI.Tests.Infrastructure;

namespace AmsaAPI.Tests.FastEndpoints;

public class GetAllUnitsEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task GetAllUnits_WithUnits_ReturnsOkWithUnitsList()
    {
        // Arrange
        await DbSeedHelper.SeedUnitsAsync(DbContext, count: 3);

        // Act
        var response = await Client.GetAsync("/api/units");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var units = await response.Content.ReadAsAsync<List<UnitSummaryDto>>();
        Assert.NotNull(units);
        Assert.Equal(3, units.Count);
    }

    [Fact]
    public async Task GetAllUnits_WithoutUnits_ReturnsOkWithEmptyList()
    {
        // Act
        var response = await Client.GetAsync("/api/units");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var units = await response.Content.ReadAsAsync<List<UnitSummaryDto>>();
        Assert.NotNull(units);
        Assert.Empty(units);
    }

    [Fact]
    public async Task GetAllUnits_ReturnsSortedByStateAndUnitName()
    {
        // Arrange
        // This test verifies the sorting order is correct
        await DbSeedHelper.SeedUnitsAsync(DbContext, count: 2);

        // Act
        var response = await Client.GetAsync("/api/units");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var units = await response.Content.ReadAsAsync<List<UnitSummaryDto>>();
        Assert.NotNull(units);
        // Verify ordering - units should be sorted by StateName then UnitName
        for (int i = 1; i < units.Count; i++)
        {
            var prevState = units[i - 1].StateName;
            var currState = units[i].StateName;
            var comparison = string.Compare(prevState, currState, StringComparison.Ordinal);

            if (comparison == 0)
            {
                // Same state, check unit name ordering
                Assert.True(string.Compare(units[i - 1].UnitName, units[i].UnitName, StringComparison.Ordinal) <= 0);
            }
            else
            {
                // Different states, previous state should come first
                Assert.True(comparison < 0);
            }
        }
    }
}

public class GetUnitByIdEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task GetUnitById_WithValidId_ReturnsOkWithUnitDetails()
    {
        // Arrange
        await DbSeedHelper.SeedUnitsAsync(DbContext, count: 1);
        var unit = DbContext.Units.First();

        // Act
        var response = await Client.GetAsync($"/api/units/{unit.UnitId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<UnitDetailResponse>();
        Assert.NotNull(result);
        Assert.Equal(unit.UnitName, result.UnitName);
    }

    [Fact]
    public async Task GetUnitById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/units/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUnitById_IncludesMemberCount()
    {
        // Arrange
        await DbSeedHelper.SeedUnitsAsync(DbContext, count: 1);
        await DbSeedHelper.SeedMembersAsync(DbContext, count: 2);
        var unit = DbContext.Units.First();

        // Act
        var response = await Client.GetAsync($"/api/units/{unit.UnitId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsAsync<UnitDetailResponse>();
        Assert.NotNull(result);
        Assert.True(result.MemberCount > 0);
    }
}
