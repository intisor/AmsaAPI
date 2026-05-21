using AmsaAPI.Data;
using AmsaAPI.DTOs;
using AmsaAPI.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.Endpoints;

public static class MemberEndpoints
{
    public static void MapMemberEndpoints(this WebApplication app)
    {
        var memberGroup = app.MapGroup("/api/minimal/members").WithTags("Members (Minimal API)");

        // Get all members with complete details
        memberGroup.MapGet("/", GetAllMembers);
        
        // Get member by ID
        memberGroup.MapGet("/{id:int}", GetMemberById);
        
        // Get member by MKAN ID
        memberGroup.MapGet("/mkan/{mkanId:int}", GetMemberByMkanId);
        
        // Get members by unit
        memberGroup.MapGet("/unit/{unitId:int}", GetMembersByUnit);
        
        // Get members by department
        memberGroup.MapGet("/department/{departmentId:int}", GetMembersByDepartment);
        
        // Search members by name
        memberGroup.MapGet("/search/{name}", SearchMembersByName);
        
        // Create new member
        memberGroup.MapPost("/", CreateMember);
        
        // Update member
        memberGroup.MapPut("/{id:int}", UpdateMember);
        
        // Delete member
        memberGroup.MapDelete("/{id:int}", DeleteMember);
    }

    private static async Task<IResult> GetAllMembers(AmsaDbContext db)
    {
        try
        {
            var members = await db.Database.SqlQueryRaw<MemberDetailResponse>("""
                SELECT 
                    m.MemberId,
                    m.FirstName,
                    m.LastName,
                    m.Email,
                    m.Phone,
                    m.Mkanid,
                    u.UnitId,
                    u.UnitName,
                    s.StateId,
                    s.StateName,
                    n.NationalId,
                    n.NationalName
                FROM Members m
                INNER JOIN Units u ON m.UnitId = u.UnitId
                INNER JOIN States s ON u.StateId = s.StateId
                INNER JOIN Nationals n ON s.NationalId = n.NationalId
                ORDER BY m.FirstName, m.LastName
                """).ToListAsync();

            return Results.Ok(members);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving members: {ex.Message}");
        }
    }

    private static async Task<IResult> GetMemberById(int id, AmsaDbContext db)
    {
        try
        {
            var member = await db.Database.SqlQueryRaw<MemberDetailResponse>("""
                SELECT 
                    m.MemberId,
                    m.FirstName,
                    m.LastName,
                    m.Email,
                    m.Phone,
                    m.Mkanid,
                    u.UnitId,
                    u.UnitName,
                    s.StateId,
                    s.StateName,
                    n.NationalId,
                    n.NationalName
                FROM Members m
                INNER JOIN Units u ON m.UnitId = u.UnitId
                INNER JOIN States s ON u.StateId = s.StateId
                INNER JOIN Nationals n ON s.NationalId = n.NationalId
                WHERE m.MemberId = {0}
                """, id).FirstOrDefaultAsync();

            if (member == null)
                return Results.NotFound($"Member with ID {id} not found");

            return Results.Ok(member);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving member: {ex.Message}");
        }
    }

    private static async Task<IResult> GetMemberByMkanId(int mkanId, AmsaDbContext db)
    {
        try
        {
            var member = await db.Database.SqlQueryRaw<MemberDetailResponse>("""
                SELECT 
                    m.MemberId,
                    m.FirstName,
                    m.LastName,
                    m.Email,
                    m.Phone,
                    m.Mkanid,
                    u.UnitId,
                    u.UnitName,
                    s.StateId,
                    s.StateName,
                    n.NationalId,
                    n.NationalName
                FROM Members m
                INNER JOIN Units u ON m.UnitId = u.UnitId
                INNER JOIN States s ON u.StateId = s.StateId
                INNER JOIN Nationals n ON s.NationalId = n.NationalId
                WHERE m.Mkanid = {0}
                """, mkanId).FirstOrDefaultAsync();

            if (member == null)
                return Results.NotFound($"Member with MKAN ID {mkanId} not found");

            return Results.Ok(member);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving member: {ex.Message}");
        }
    }

    private static async Task<IResult> GetMembersByUnit(int unitId, AmsaDbContext db)
    {
        try
        {
            var members = await db.Database.SqlQueryRaw<MemberSummaryResponse>("""
                SELECT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid
                FROM Members m
                WHERE m.UnitId = {0}
                ORDER BY m.FirstName, m.LastName
                """, unitId).ToListAsync();

            return members.Count != 0
                ? Results.Ok(members)
                : Results.NotFound($"No members found for unit {unitId}");
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving members by unit: {ex.Message}");
        }
    }

    private static async Task<IResult> GetMembersByDepartment(int departmentId, AmsaDbContext db)
    {
        try
        {
            var members = await db.Database.SqlQueryRaw<MemberSummaryResponse>("""
                SELECT DISTINCT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid
                FROM Members m
                INNER JOIN MemberLevelDepartments mld ON m.MemberId = mld.MemberId
                INNER JOIN LevelDepartments ld ON mld.LevelDepartmentId = ld.LevelDepartmentId
                WHERE ld.DepartmentId = {0}
                ORDER BY m.FirstName, m.LastName
                """, departmentId).ToListAsync();

            return Results.Ok(members);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error retrieving members by department: {ex.Message}");
        }
    }

    private static async Task<IResult> SearchMembersByName(string name, AmsaDbContext db)
    {
        try
        {
            var pattern = $"%{name}%";
            var members = await db.Database.SqlQueryRaw<MemberSummaryResponse>("""
                SELECT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid
                FROM Members m
                WHERE m.FirstName LIKE {0} COLLATE SQL_Latin1_General_CP1_CI_AI
                   OR m.LastName LIKE {0} COLLATE SQL_Latin1_General_CP1_CI_AI
                ORDER BY m.FirstName, m.LastName
                """, pattern).ToListAsync();

            return Results.Ok(members);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error searching members: {ex.Message}");
        }
    }

    private static async Task<IResult> CreateMember(CreateMemberRequest request, AmsaDbContext db)
    {
        try
        {
            // Check if MKAN ID already exists using raw SQL
            var existingMember = await db.Database.SqlQueryRaw<int>("""
                SELECT m.MemberId
                FROM Members m
                WHERE m.Mkanid = {0}
                """, request.Mkanid).FirstOrDefaultAsync();

            if (existingMember != 0)
                return Results.BadRequest($"Member with MKAN ID {request.Mkanid} already exists");

            // Validate UnitId exists
            var unitExists = await db.Database.SqlQueryRaw<int>("""
                SELECT COUNT(*) FROM Units
                WHERE UnitId = {0}
                """, request.UnitId).FirstOrDefaultAsync();

            if (unitExists == 0)
                return Results.BadRequest($"Unit with ID {request.UnitId} does not exist");

            var member = request.ToEntity();
            db.Members.Add(member);
            await db.SaveChangesAsync();

            return Results.Created($"/api/minimal/members/{member.MemberId}",
                new
                {
                    member.MemberId,
                    member.FirstName,
                    member.LastName,
                    member.Mkanid
                });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to create member: {ex.Message}");
        }
    }

    private static async Task<IResult> UpdateMember(int id, UpdateMemberRequest request, AmsaDbContext db)
    {
        try
        {
            // Get member using raw SQL
            var member = await db.Database.SqlQueryRaw<int>("""
                SELECT m.MemberId
                FROM Members m
                WHERE m.MemberId = {0}
                """, id).FirstOrDefaultAsync();

            if (member == 0)
                return Results.NotFound($"Member with ID {id} not found");

            // Validate UnitId exists if changed
            var currentUnit = await db.Database.SqlQueryRaw<int>("""
                SELECT m.UnitId
                FROM Members m
                WHERE m.MemberId = {0}
                """, id).FirstOrDefaultAsync();

            if (currentUnit != request.UnitId)
            {
                var unitExists = await db.Database.SqlQueryRaw<int>("""
                    SELECT COUNT(*) FROM Units
                    WHERE UnitId = {0}
                    """, request.UnitId).FirstOrDefaultAsync();

                if (unitExists == 0)
                    return Results.BadRequest($"Unit with ID {request.UnitId} does not exist");
            }

            // Update using raw SQL
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE Members
                SET FirstName = {request.FirstName},
                    LastName = {request.LastName},
                    Email = {request.Email},
                    Phone = {request.Phone},
                    UnitId = {request.UnitId}
                WHERE MemberId = {id}
                """);

            return Results.Ok(new
            {
                MemberId = id,
                request.FirstName,
                request.LastName,
                request.UnitId
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to update member: {ex.Message}");
        }
    }

    private static async Task<IResult> DeleteMember(int id, AmsaDbContext db)
    {
        try
        {
            // Get member using raw SQL
            var member = await db.Database.SqlQueryRaw<(int MemberId, string FirstName, string LastName)>("""
                SELECT m.MemberId, m.FirstName, m.LastName
                FROM Members m
                WHERE m.MemberId = {0}
                """, id).FirstOrDefaultAsync();

            if (member.MemberId == 0)
                return Results.NotFound($"Member with ID {id} not found");

            // Delete using raw SQL
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM Members
                WHERE MemberId = {id}
                """);

            return Results.Ok(new
            {
                Message = $"Member {member.FirstName} {member.LastName} deleted successfully"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to delete member: {ex.Message}");
        }
    }
}