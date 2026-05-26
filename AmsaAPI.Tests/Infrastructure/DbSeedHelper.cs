using AmsaAPI.Data;

namespace AmsaAPI.Tests.Infrastructure;

/// <summary>
/// Helper for seeding test data into the database
/// </summary>
public static class DbSeedHelper
{
    public static async Task SeedMembersAsync(AmsaDbContext context, int count = 5)
    {
        // Ensure National exists
        var national = context.Nationals.FirstOrDefault() ?? new National { NationalId = 1, NationalName = "Test National" };
        if (national.NationalId == 0)
        {
            context.Nationals.Add(national);
            await context.SaveChangesAsync();
        }

        // Ensure State exists
        var state = context.States.FirstOrDefault() ?? new State { StateId = 1, StateName = "Test State", NationalId = national.NationalId };
        if (state.StateId == 0)
        {
            context.States.Add(state);
            await context.SaveChangesAsync();
        }

        // Ensure Unit exists
        var unit = context.Units.FirstOrDefault() ?? new Unit { UnitId = 1, UnitName = "Test Unit", StateId = state.StateId };
        if (unit.UnitId == 0)
        {
            context.Units.Add(unit);
            await context.SaveChangesAsync();
        }

        // Add members
        for (int i = 1; i <= count; i++)
        {
            context.Members.Add(new Member
            {
                FirstName = $"Test{i}",
                LastName = $"Member{i}",
                Mkanid = 1000 + i,
                UnitId = unit.UnitId,
                Email = $"test{i}@example.com",
                Phone = $"0800000000{i}"
            });
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedUnitsAsync(AmsaDbContext context, int count = 3)
    {
        // Ensure National exists
        var national = context.Nationals.FirstOrDefault() ?? new National { NationalId = 1, NationalName = "Test National" };
        if (national.NationalId == 0)
        {
            context.Nationals.Add(national);
            await context.SaveChangesAsync();
        }

        // Ensure State exists
        var state = context.States.FirstOrDefault() ?? new State { StateId = 1, StateName = "Test State", NationalId = national.NationalId };
        if (state.StateId == 0)
        {
            context.States.Add(state);
            await context.SaveChangesAsync();
        }

        // Add units
        for (int i = 1; i <= count; i++)
        {
            context.Units.Add(new Unit
            {
                UnitName = $"Test Unit {i}",
                StateId = state.StateId
            });
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedDepartmentsAsync(AmsaDbContext context, int count = 4)
    {
        // Add departments
        for (int i = 1; i <= count; i++)
        {
            context.Departments.Add(new Department
            {
                DepartmentName = $"Test Department {i}"
            });
        }

        await context.SaveChangesAsync();
    }
}
