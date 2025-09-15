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
            var membersWithHierarchy = await db.Members
                .Include(m => m.Unit.State.National)
                .AsNoTracking()
                .ToListAsync();

            var rolesData = await db.MemberLevelDepartments
                .Include(mld => mld.LevelDepartment.Department)
                .Include(mld => mld.LevelDepartment.Level)
                .AsNoTracking()
                .ToListAsync();

            var response = membersWithHierarchy.Select(member => 
            {
                var memberRoles = rolesData.Where(role => role.MemberId == member.MemberId).ToList();
                return member.ToDetailResponseWithRoles(memberRoles);
            }).ToList();

            return Results.Ok(response);
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
            var member = await db.Members
                .Include(m => m.Unit.State.National)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null)
                return Results.NotFound($"Member with ID {id} not found");

            var rolesData = await db.MemberLevelDepartments
                .Where(mld => mld.MemberId == id)
                .Include(mld => mld.LevelDepartment.Department)
                .Include(mld => mld.LevelDepartment.Level)
                .AsNoTracking()
                .ToListAsync();

            var response = member.ToDetailResponseWithRoles(rolesData);
            return Results.Ok(response);
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
            var member = await db.Members
                .Include(m => m.Unit.State.National)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Mkanid == mkanId);

            if (member == null)
                return Results.NotFound($"Member with MKAN ID {mkanId} not found");

            var rolesData = await db.MemberLevelDepartments
                .Where(mld => mld.MemberId == member.MemberId)
                .Include(mld => mld.LevelDepartment.Department)
                .Include(mld => mld.LevelDepartment.Level)
                .AsNoTracking()
                .ToListAsync();

            var response = member.ToDetailResponseWithRoles(rolesData);
            return Results.Ok(response);
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
            var members = await db.Members
                .Where(m => m.UnitId == unitId)
                .Select(m => new MemberSummaryResponse
                {
                    MemberId = m.MemberId,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Email = m.Email,
                    Phone = m.Phone,
                    Mkanid = m.Mkanid
                })
                .ToListAsync();

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
            var members = await db.Members
                .Where(m => m.MemberLevelDepartments.Any(mld => 
                    mld.LevelDepartment.DepartmentId == departmentId))
                .Select(m => new MemberSummaryResponse
                {
                    MemberId = m.MemberId,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Email = m.Email,
                    Phone = m.Phone,
                    Mkanid = m.Mkanid
                })
                .ToListAsync();

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
            var members = await db.Members
                .Where(m => m.FirstName.Contains(name) || m.LastName.Contains(name))
                .Select(m => new MemberSummaryResponse
                {
                    MemberId = m.MemberId,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Email = m.Email,
                    Phone = m.Phone,
                    Mkanid = m.Mkanid
                })
                .ToListAsync();

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
            // Check if MKAN ID already exists
            var existingMember = await db.Members
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Mkanid == request.Mkanid);
            
            if (existingMember != null)
                return Results.BadRequest($"Member with MKAN ID {request.Mkanid} already exists");

            // Validate UnitId exists
            var unitExists = await db.Units
                .AsNoTracking()
                .AnyAsync(u => u.UnitId == request.UnitId);
            
            if (!unitExists)
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
            var member = await db.Members.FindAsync(id);
            if (member == null)
                return Results.NotFound($"Member with ID {id} not found");

            // Validate UnitId exists if changed
            if (member.UnitId != request.UnitId)
            {
                var unitExists = await db.Units
                    .AsNoTracking()
                    .AnyAsync(u => u.UnitId == request.UnitId);
                
                if (!unitExists)
                    return Results.BadRequest($"Unit with ID {request.UnitId} does not exist");
            }

            request.UpdateEntity(member);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                member.MemberId,
                member.FirstName,
                member.LastName,
                member.Mkanid
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
            var member = await db.Members.FindAsync(id);
            if (member == null)
                return Results.NotFound($"Member with ID {id} not found");

            db.Members.Remove(member);
            await db.SaveChangesAsync();
            
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