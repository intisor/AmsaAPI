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
                // Get member data with organizational hierarchy
                var memberData = await db.Database.SqlQueryRaw<MemberWithHierarchyDto>("""
                    SELECT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid,
                           u.UnitId, u.UnitName, u.StateId,
                           s.StateName, s.NationalId,
                           n.NationalName
                    FROM Members m
                    INNER JOIN Units u ON m.UnitId = u.UnitId
                    INNER JOIN States s ON u.StateId = s.StateId
                    INNER JOIN Nationals n ON s.NationalId = n.NationalId
                    """).ToListAsync();

                // Get roles data separately for better performance
                var rolesData = await db.Database.SqlQueryRaw<MemberRoleQueryDto>("""
                    SELECT mld.MemberId,
                           d.DepartmentName,
                           l.LevelType,
                           CASE 
                               WHEN l.NationalId IS NOT NULL THEN 'National'
                               WHEN l.StateId IS NOT NULL THEN 'State'
                               WHEN l.UnitId IS NOT NULL THEN 'Unit'
                               ELSE 'Unknown'
                           END as Scope
                    FROM MemberLevelDepartments mld
                    INNER JOIN LevelDepartments ld ON mld.LevelDepartmentId = ld.LevelDepartmentId
                    INNER JOIN Departments d ON ld.DepartmentId = d.DepartmentId
                    INNER JOIN Levels l ON ld.LevelId = l.LevelId
                    """).ToListAsync();

                // Combine member data with their roles
                var response = memberData.Select(member => new MemberDetailResponse
                {
                    MemberId = member.MemberId,
                    FirstName = member.FirstName,
                    LastName = member.LastName,
                    Email = member.Email,
                    Phone = member.Phone,
                    Mkanid = member.Mkanid,
                    Unit = new UnitHierarchyDto
                    {
                        UnitId = member.UnitId,
                        UnitName = member.UnitName,
                        State = new StateHierarchyDto
                        {
                            StateId = member.StateId,
                            StateName = member.StateName,
                            National = new NationalDto
                            {
                                NationalId = member.NationalId,
                                NationalName = member.NationalName
                            }
                        }
                    },
                    Roles = rolesData.Where(role => role.MemberId == member.MemberId)
                                    .Select(role => new MemberRoleDto
                                    {
                                        DepartmentName = role.DepartmentName,
                                        LevelType = role.LevelType,
                                        Scope = role.Scope
                                    }).ToList()
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
                var memberData = await db.Database.SqlQueryRaw<MemberWithHierarchyDto>("""
                    SELECT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid,
                           u.UnitId, u.UnitName, u.StateId,
                           s.StateName, s.NationalId,
                           n.NationalName
                    FROM Members m
                    INNER JOIN Units u ON m.UnitId = u.UnitId
                    INNER JOIN States s ON u.StateId = s.StateId
                    INNER JOIN Nationals n ON s.NationalId = n.NationalId
                    WHERE m.MemberId = {0}
                    """, id).ToListAsync();

                var member = memberData.FirstOrDefault();
                if (member == null)
                    return Results.NotFound($"Member with ID {id} not found");

                var rolesData = await db.Database.SqlQueryRaw<MemberRoleQueryDto>("""
                    SELECT mld.MemberId,
                           d.DepartmentName,
                           l.LevelType,
                           CASE 
                               WHEN l.NationalId IS NOT NULL THEN 'National'
                               WHEN l.StateId IS NOT NULL THEN 'State'
                               WHEN l.UnitId IS NOT NULL THEN 'Unit'
                               ELSE 'Unknown'
                           END as Scope
                    FROM MemberLevelDepartments mld
                    INNER JOIN LevelDepartments ld ON mld.LevelDepartmentId = ld.LevelDepartmentId
                    INNER JOIN Departments d ON ld.DepartmentId = d.DepartmentId
                    INNER JOIN Levels l ON ld.LevelId = l.LevelId
                    WHERE mld.MemberId = {0}
                    """, id).ToListAsync();

                var response = new MemberDetailResponse
                {
                    MemberId = member.MemberId,
                    FirstName = member.FirstName,
                    LastName = member.LastName,
                    Email = member.Email,
                    Phone = member.Phone,
                    Mkanid = member.Mkanid,
                    Unit = new UnitHierarchyDto
                    {
                        UnitId = member.UnitId,
                        UnitName = member.UnitName,
                        State = new StateHierarchyDto
                        {
                            StateId = member.StateId,
                            StateName = member.StateName,
                            National = new NationalDto
                            {
                                NationalId = member.NationalId,
                                NationalName = member.NationalName
                            }
                        }
                    },
                    Roles = rolesData.Select(role => new MemberRoleDto
                    {
                        DepartmentName = role.DepartmentName,
                        LevelType = role.LevelType,
                        Scope = role.Scope
                    }).ToList()
                };

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
                var memberData = await db.Database.SqlQueryRaw<MemberWithHierarchyDto>("""
                    SELECT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid,
                           u.UnitId, u.UnitName, u.StateId,
                           s.StateName, s.NationalId,
                           n.NationalName
                    FROM Members m
                    INNER JOIN Units u ON m.UnitId = u.UnitId
                    INNER JOIN States s ON u.StateId = s.StateId
                    INNER JOIN Nationals n ON s.NationalId = n.NationalId
                    WHERE m.Mkanid = {0}
                    """, mkanId).ToListAsync();

                var member = memberData.FirstOrDefault();
                if (member == null)
                    return Results.NotFound($"Member with MKAN ID {mkanId} not found");

                var rolesData = await db.Database.SqlQueryRaw<MemberRoleQueryDto>("""
                    SELECT mld.MemberId,
                           d.DepartmentName,
                           l.LevelType,
                           CASE 
                               WHEN l.NationalId IS NOT NULL THEN 'National'
                               WHEN l.StateId IS NOT NULL THEN 'State'
                               WHEN l.UnitId IS NOT NULL THEN 'Unit'
                               ELSE 'Unknown'
                           END as Scope
                    FROM MemberLevelDepartments mld
                    INNER JOIN LevelDepartments ld ON mld.LevelDepartmentId = ld.LevelDepartmentId
                    INNER JOIN Departments d ON ld.DepartmentId = d.DepartmentId
                    INNER JOIN Levels l ON ld.LevelId = l.LevelId
                    WHERE mld.MemberId = {0}
                    """, member.MemberId).ToListAsync();

                var response = new MemberDetailResponse
                {
                    MemberId = member.MemberId,
                    FirstName = member.FirstName,
                    LastName = member.LastName,
                    Email = member.Email,
                    Phone = member.Phone,
                    Mkanid = member.Mkanid,
                    Unit = new UnitHierarchyDto
                    {
                        UnitId = member.UnitId,
                        UnitName = member.UnitName,
                        State = new StateHierarchyDto
                        {
                            StateId = member.StateId,
                            StateName = member.StateName,
                            National = new NationalDto
                            {
                                NationalId = member.NationalId,
                                NationalName = member.NationalName
                            }
                        }
                    },
                    Roles = rolesData.Select(role => new MemberRoleDto
                    {
                        DepartmentName = role.DepartmentName,
                        LevelType = role.LevelType,
                        Scope = role.Scope
                    }).ToList()
                };

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

                return members.Any()
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

    // Helper DTOs for raw SQL queries
    public class MemberWithHierarchyDto
    {
        public int MemberId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int Mkanid { get; set; }
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public int StateId { get; set; }
        public string StateName { get; set; } = string.Empty;
        public int NationalId { get; set; }
        public string NationalName { get; set; } = string.Empty;
    }

    public class MemberRoleQueryDto
    {
        public int MemberId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string LevelType { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
    }
}