using AmsaAPI.Data;
using AmsaAPI.DTOs;
using AmsaAPI.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.Endpoints
{
    public static class MemberEndpoints
    {
        public static void MapMemberEndpoints(this WebApplication app)
        {
            var memberGroup = app.MapGroup("/api/members").WithTags("Members");

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
                // Get member data with organizational hierarchy using FromSqlRaw
                var membersWithHierarchy = await db.Members
                    .FromSqlRaw("""
                        SELECT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid, m.UnitId
                        FROM Members m
                        """)
                    .Include(m => m.Unit)
                        .ThenInclude(u => u.State)
                            .ThenInclude(s => s.National)
                    .AsNoTracking()
                    .ToListAsync();

                // Get roles data using FromSqlRaw on MemberLevelDepartments
                var rolesData = await db.MemberLevelDepartments
                    .FromSqlRaw("""
                        SELECT mld.MemberLevelDepartmentId, mld.MemberId, mld.LevelDepartmentId
                        FROM MemberLevelDepartments mld
                        """)
                    .Include(mld => mld.LevelDepartment)
                        .ThenInclude(ld => ld.Department)
                    .Include(mld => mld.LevelDepartment)
                        .ThenInclude(ld => ld.Level)
                    .AsNoTracking()
                    .ToListAsync();

                // Transform to response DTOs using extension methods
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
                // Using FromSqlRaw with Include for proper navigation property loading
                var member = await db.Members
                    .FromSqlRaw("""
                        SELECT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid, m.UnitId
                        FROM Members m
                        WHERE m.MemberId = {0}
                        """, id)
                    .Include(m => m.Unit)
                        .ThenInclude(u => u.State)
                            .ThenInclude(s => s.National)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (member == null)
                    return Results.NotFound($"Member with ID {id} not found");

                // Get roles using FromSqlRaw on MemberLevelDepartments
                var rolesData = await db.MemberLevelDepartments
                    .FromSqlRaw("""
                        SELECT mld.MemberLevelDepartmentId, mld.MemberId, mld.LevelDepartmentId
                        FROM MemberLevelDepartments mld
                        WHERE mld.MemberId = {0}
                        """, id)
                    .Include(mld => mld.LevelDepartment)
                        .ThenInclude(ld => ld.Department)
                    .Include(mld => mld.LevelDepartment)
                        .ThenInclude(ld => ld.Level)
                    .AsNoTracking()
                    .ToListAsync();

                // Use extension method for transformation
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
                // Using FromSqlRaw with Include for proper navigation property loading
                var member = await db.Members
                    .FromSqlRaw("""
                        SELECT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid, m.UnitId
                        FROM Members m
                        WHERE m.Mkanid = {0}
                        """, mkanId)
                    .Include(m => m.Unit)
                        .ThenInclude(u => u.State)
                            .ThenInclude(s => s.National)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (member == null)
                    return Results.NotFound($"Member with MKAN ID {mkanId} not found");

                // Get roles using FromSqlRaw on MemberLevelDepartments
                var rolesData = await db.MemberLevelDepartments
                    .FromSqlRaw("""
                        SELECT mld.MemberLevelDepartmentId, mld.MemberId, mld.LevelDepartmentId
                        FROM MemberLevelDepartments mld
                        WHERE mld.MemberId = {0}
                        """, member.MemberId)
                    .Include(mld => mld.LevelDepartment)
                        .ThenInclude(ld => ld.Department)
                    .Include(mld => mld.LevelDepartment)
                        .ThenInclude(ld => ld.Level)
                    .AsNoTracking()
                    .ToListAsync();

                // Use extension method for transformation
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
                    .FromSqlRaw("""
                        SELECT MemberId, FirstName, LastName, Email, Phone, Mkanid, UnitId
                        FROM Members
                        WHERE UnitId = {0}
                        """, unitId)
                    .AsNoTracking()
                    .Select(m => new
                    {
                        m.MemberId,
                        m.FirstName,
                        m.LastName,
                        m.Email,
                        m.Phone,
                        m.Mkanid
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
                    .FromSqlRaw("""
                        SELECT DISTINCT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid, m.UnitId
                        FROM Members m
                        INNER JOIN MemberLevelDepartments mld ON m.MemberId = mld.MemberId
                        INNER JOIN LevelDepartments ld ON mld.LevelDepartmentId = ld.LevelDepartmentId
                        WHERE ld.DepartmentId = {0}
                        """, departmentId)
                    .AsNoTracking()
                    .Select(m => new
                    {
                        m.MemberId,
                        m.FirstName,
                        m.LastName,
                        m.Email,
                        m.Phone,
                        m.Mkanid
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
                    .FromSqlRaw("""
                        SELECT MemberId, FirstName, LastName, Email, Phone, Mkanid, UnitId
                        FROM Members
                        WHERE FirstName LIKE '%' + {0} + '%' OR LastName LIKE '%' + {0} + '%'
                        """, name)
                    .AsNoTracking()
                    .Select(m => new
                    {
                        m.MemberId,
                        m.FirstName,
                        m.LastName,
                        m.Email,
                        m.Phone,
                        m.Mkanid
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

                return Results.Created($"/api/members/{member.MemberId}",
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
}