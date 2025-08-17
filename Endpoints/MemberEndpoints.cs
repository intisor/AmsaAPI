using AmsaAPI;
using AmsaAPI.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AmsaNigeriaApi.Endpoints
{
    public static class MemberEndpoints
    {
        public static void MapMemberEndpoints(this WebApplication app)
        {
            // =========================== MEMBER ENDPOINTS ===========================
            
            // Get all members with complete details
            app.MapGet("/api/members", async (AmsaDbContext db) =>
            {
                var members = await db.Members
                    .Include(m => m.Unit)
                        .ThenInclude(u => u.State)
                            .ThenInclude(s => s.National)
                    .Include(m => m.MemberLevelDepartments)
                        .ThenInclude(mld => mld.LevelDepartment)
                            .ThenInclude(ld => ld.Department)
                    .Select(m => new
                    {
                        m.MemberId,
                        m.FirstName,
                        m.LastName,
                        m.Email,
                        m.Phone,
                        m.Mkanid,
                        Unit = new
                        {
                            m.Unit.UnitId,
                            m.Unit.UnitName,
                            State = new
                            {
                                m.Unit.State.StateId,
                                m.Unit.State.StateName,
                                National = new
                                {
                                    m.Unit.State.National.NationalId,
                                    m.Unit.State.National.NationalName
                                }
                            }
                        },
                        Roles = m.MemberLevelDepartments.Select(mld => new
                        {
                            mld.LevelDepartment.Department.DepartmentName,
                            LevelType = mld.LevelDepartment.Level.LevelType
                        })
                    })
                    .ToListAsync();
                
                return Results.Ok(members);
            });

            // Get member by ID with complete details
            app.MapGet("/api/members/{id}", async (int id, AmsaDbContext db) =>
            {
                var member = await db.Members
                    .Include(m => m.Unit)
                        .ThenInclude(u => u.State)
                            .ThenInclude(s => s.National)
                    .Include(m => m.MemberLevelDepartments)
                        .ThenInclude(mld => mld.LevelDepartment)
                            .ThenInclude(ld => ld.Department)
                    .Where(m => m.MemberId == id)
                    .Select(m => new
                    {
                        m.MemberId,
                        m.FirstName,
                        m.LastName,
                        m.Email,
                        m.Phone,
                        m.Mkanid,
                        Unit = new
                        {
                            m.Unit.UnitId,
                            m.Unit.UnitName,
                            State = new
                            {
                                m.Unit.State.StateId,
                                m.Unit.State.StateName,
                                National = new
                                {
                                    m.Unit.State.National.NationalId,
                                    m.Unit.State.National.NationalName
                                }
                            }
                        },
                        Roles = m.MemberLevelDepartments.Select(mld => new
                        {
                            mld.LevelDepartment.Department.DepartmentName,
                            LevelType = mld.LevelDepartment.Level.LevelType
                        })
                    })
                    .FirstOrDefaultAsync();
                
                return member is not null ? Results.Ok(member) : Results.NotFound();
            });
            
            // Get members by unit
            app.MapGet("/api/members/unit/{unitId}", async (int unitId, AmsaDbContext db) =>
            {
                var members = await db.Members
                    .Where(m => m.UnitId == unitId)
                    .Select(m => new { m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid })
                    .ToListAsync();
                
                return members.Any() ? Results.Ok(members) : Results.NotFound($"No members found for unit {unitId}");
            });

            // Get members by department
            app.MapGet("/api/members/department/{departmentId}", async (int departmentId, AmsaDbContext db) =>
            {
                var members = await db.Members
                    .Include(m => m.MemberLevelDepartments)
                        .ThenInclude(mld => mld.LevelDepartment)
                    .Where(m => m.MemberLevelDepartments.Any(mld => mld.LevelDepartment.DepartmentId == departmentId))
                    .Select(m => new { m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid })
                    .ToListAsync();
                
                return Results.Ok(members);
            });

            // Get member by MKAN ID
            app.MapGet("/api/members/mkan/{mkanId}", async (int mkanId, AmsaDbContext db) =>
            {
                var member = await db.Members
                    .Include(m => m.Unit).ThenInclude(u => u.State).ThenInclude(s => s.National)
                    .Include(m => m.MemberLevelDepartments).ThenInclude(mld => mld.LevelDepartment).ThenInclude(ld => ld.Department)
                    .Where(m => m.Mkanid == mkanId)
                    .Select(m => new
                    {
                        m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid,
                        Unit = new { m.Unit.UnitId, m.Unit.UnitName, State = m.Unit.State.StateName, National = m.Unit.State.National.NationalName },
                        Roles = m.MemberLevelDepartments.Select(mld => mld.LevelDepartment.Department.DepartmentName)
                    })
                    .FirstOrDefaultAsync();
                
                return member != null ? Results.Ok(member) : Results.NotFound($"Member with MKAN ID {mkanId} not found");
            });

            // Search members by name
            app.MapGet("/api/members/search/{name}", async (string name, AmsaDbContext db) =>
            {
                var members = await db.Members
                    .Where(m => m.FirstName.Contains(name) || m.LastName.Contains(name))
                    .Select(m => new { m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid })
                    .ToListAsync();
                
                return Results.Ok(members);
            });

            // Create new member
            app.MapPost("/api/members", async (Member member, AmsaDbContext db) =>
            {
                try
                {
                    // Check if MKAN ID already exists
                    if (await db.Members.AnyAsync(m => m.Mkanid == member.Mkanid))
                        return Results.BadRequest($"Member with MKAN ID {member.Mkanid} already exists");

                    db.Members.Add(member);
                    await db.SaveChangesAsync();
                    return Results.Created($"/api/members/{member.MemberId}", member);
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Failed to create member: {ex.Message}");
                }
            });

            // Update member
            app.MapPut("/api/members/{id}", async (int id, Member updatedMember, AmsaDbContext db) =>
            {
                var member = await db.Members.FindAsync(id);
                if (member == null) return Results.NotFound();

                member.FirstName = updatedMember.FirstName;
                member.LastName = updatedMember.LastName;
                member.Email = updatedMember.Email;
                member.Phone = updatedMember.Phone;
                member.UnitId = updatedMember.UnitId;

                await db.SaveChangesAsync();
                return Results.Ok(member);
            });

            // Delete member
            app.MapDelete("/api/members/{id}", async (int id, AmsaDbContext db) =>
            {
                var member = await db.Members.FindAsync(id);
                if (member == null) return Results.NotFound();

                db.Members.Remove(member);
                await db.SaveChangesAsync();
                return Results.Ok(new { Message = $"Member {member.FirstName} {member.LastName} deleted successfully" });
            });

            // =========================== DEPARTMENT ENDPOINTS ===========================
            
            // Get all departments
            app.MapGet("/api/departments", async (AmsaDbContext db) =>
            {
                var departments = await db.Departments
                    .Select(d => new
                    {
                        d.DepartmentId,
                        d.DepartmentName
                    })
                    .ToListAsync();
                
                return Results.Ok(departments);
            });
            
            // Create department
            app.MapPost("/api/departments", async (Department department, AmsaDbContext db) =>
            {
                db.Departments.Add(department);
                await db.SaveChangesAsync();
                return Results.Created($"/api/departments/{department.DepartmentId}", department);
            });

            // Update department
            app.MapPut("/api/departments/{id}", async (int id, Department updatedDept, AmsaDbContext db) =>
            {
                var department = await db.Departments.FindAsync(id);
                if (department == null) return Results.NotFound();

                department.DepartmentName = updatedDept.DepartmentName;
                await db.SaveChangesAsync();
                return Results.Ok(department);
            });

            // Delete department
            app.MapDelete("/api/departments/{id}", async (int id, AmsaDbContext db) =>
            {
                var department = await db.Departments.FindAsync(id);
                if (department == null) return Results.NotFound();

                db.Departments.Remove(department);
                await db.SaveChangesAsync();
                return Results.Ok(new { Message = $"Department {department.DepartmentName} deleted successfully" });
            });

            // Get department by ID
            app.MapGet("/api/departments/{id}", async (int id, AmsaDbContext db) =>
            {
                var department = await db.Departments
                    .Include(d => d.LevelDepartments)
                        .ThenInclude(ld => ld.MemberLevelDepartments)
                            .ThenInclude(mld => mld.Member)
                    .Where(d => d.DepartmentId == id)
                    .Select(d => new
                    {
                        d.DepartmentId,
                        d.DepartmentName,
                        MemberCount = d.LevelDepartments.SelectMany(ld => ld.MemberLevelDepartments).Count(),
                        Members = d.LevelDepartments.SelectMany(ld => ld.MemberLevelDepartments)
                            .Select(mld => new { mld.Member.FirstName, mld.Member.LastName, mld.Member.Mkanid })
                    })
                    .FirstOrDefaultAsync();
                
                return department != null ? Results.Ok(department) : Results.NotFound();
            });

            // =========================== UNIT ENDPOINTS ===========================
            
            // Get all units
            app.MapGet("/api/units", async (AmsaDbContext db) =>
            {
                var units = await db.Units
                    .Include(u => u.State)
                        .ThenInclude(s => s.National)
                    .Select(u => new
                    {
                        u.UnitId,
                        u.UnitName,
                        State = new
                        {
                            u.State.StateId,
                            u.State.StateName,
                            National = new
                            {
                                u.State.National.NationalId,
                                u.State.National.NationalName
                            }
                        }
                    })
                    .ToListAsync();
                
                return Results.Ok(units);
            });
            
            // Create unit
            app.MapPost("/api/units", async (Unit unit, AmsaDbContext db) =>
            {
                db.Units.Add(unit);
                await db.SaveChangesAsync();
                return Results.Created($"/api/units/{unit.UnitId}", unit);
            });

            // Update unit
            app.MapPut("/api/units/{id}", async (int id, Unit updatedUnit, AmsaDbContext db) =>
            {
                var unit = await db.Units.FindAsync(id);
                if (unit == null) return Results.NotFound();

                unit.UnitName = updatedUnit.UnitName;
                unit.StateId = updatedUnit.StateId;
                await db.SaveChangesAsync();
                return Results.Ok(unit);
            });

            // Delete unit
            app.MapDelete("/api/units/{id}", async (int id, AmsaDbContext db) =>
            {
                var unit = await db.Units.FindAsync(id);
                if (unit == null) return Results.NotFound();

                db.Units.Remove(unit);
                await db.SaveChangesAsync();
                return Results.Ok(new { Message = $"Unit {unit.UnitName} deleted successfully" });
            });

            // Get unit by ID with members
            app.MapGet("/api/units/{id}", async (int id, AmsaDbContext db) =>
            {
                var unit = await db.Units
                    .Include(u => u.State).ThenInclude(s => s.National)
                    .Include(u => u.Members)
                    .Where(u => u.UnitId == id)
                    .Select(u => new
                    {
                        u.UnitId, u.UnitName,
                        State = new { u.State.StateId, u.State.StateName, National = u.State.National.NationalName },
                        MemberCount = u.Members.Count(),
                        Members = u.Members.Select(m => new { m.FirstName, m.LastName, m.Mkanid })
                    })
                    .FirstOrDefaultAsync();
                
                return unit != null ? Results.Ok(unit) : Results.NotFound();
            });

            // Get units by state
            app.MapGet("/api/units/state/{stateId}", async (int stateId, AmsaDbContext db) =>
            {
                var units = await db.Units
                    .Where(u => u.StateId == stateId)
                    .Select(u => new { u.UnitId, u.UnitName, MemberCount = u.Members.Count() })
                    .ToListAsync();
                
                return Results.Ok(units);
            });

            // =========================== STATE ENDPOINTS ===========================
            
            // Get all states
            app.MapGet("/api/states", async (AmsaDbContext db) =>
            {
                var states = await db.States
                    .Include(s => s.National)
                    .Select(s => new { s.StateId, s.StateName, National = s.National.NationalName, UnitCount = s.Units.Count() })
                    .ToListAsync();
                
                return Results.Ok(states);
            });

            // Get state by ID
            app.MapGet("/api/states/{id}", async (int id, AmsaDbContext db) =>
            {
                var state = await db.States
                    .Include(s => s.National)
                    .Include(s => s.Units)
                    .Where(s => s.StateId == id)
                    .Select(s => new
                    {
                        s.StateId, s.StateName,
                        National = new { s.National.NationalId, s.National.NationalName },
                        UnitCount = s.Units.Count(),
                        Units = s.Units.Select(u => new { u.UnitId, u.UnitName })
                    })
                    .FirstOrDefaultAsync();
                
                return state != null ? Results.Ok(state) : Results.NotFound();
            });

            // Create state
            app.MapPost("/api/states", async (State state, AmsaDbContext db) =>
            {
                db.States.Add(state);
                await db.SaveChangesAsync();
                return Results.Created($"/api/states/{state.StateId}", state);
            });

            // =========================== NATIONAL ENDPOINTS ===========================
            
            // Get all nationals
            app.MapGet("/api/nationals", async (AmsaDbContext db) =>
            {
                var nationals = await db.Nationals
                    .Select(n => new { n.NationalId, n.NationalName, StateCount = n.States.Count() })
                    .ToListAsync();
                
                return Results.Ok(nationals);
            });

            // Get national by ID
            app.MapGet("/api/nationals/{id}", async (int id, AmsaDbContext db) =>
            {
                var national = await db.Nationals
                    .Include(n => n.States)
                    .Where(n => n.NationalId == id)
                    .Select(n => new
                    {
                        n.NationalId, n.NationalName,
                        StateCount = n.States.Count(),
                        States = n.States.Select(s => new { s.StateId, s.StateName })
                    })
                    .FirstOrDefaultAsync();
                
                return national != null ? Results.Ok(national) : Results.NotFound();
            });

            // =========================== LEVEL ENDPOINTS ===========================
            
            // Get all levels
            app.MapGet("/api/levels", async (AmsaDbContext db) =>
            {
                var levels = await db.Levels
                    .Select(l => new { l.LevelId, l.LevelType, DepartmentCount = l.LevelDepartments.Count() })
                    .ToListAsync();
                
                return Results.Ok(levels);
            });

            // Get level departments
            app.MapGet("/api/levels/{id}/departments", async (int id, AmsaDbContext db) =>
            {
                var departments = await db.LevelDepartments
                    .Include(ld => ld.Department)
                    .Where(ld => ld.LevelId == id)
                    .Select(ld => new
                    {
                        ld.LevelDepartmentId,
                        Department = new { ld.Department.DepartmentId, ld.Department.DepartmentName },
                        MemberCount = ld.MemberLevelDepartments.Count()
                    })
                    .ToListAsync();
                
                return Results.Ok(departments);
            });

            // =========================== ROLE ASSIGNMENT ENDPOINTS ===========================
            
            // Assign role to member
            app.MapPost("/api/members/{memberId}/roles/{levelDepartmentId}", async (int memberId, int levelDepartmentId, AmsaDbContext db) =>
            {
                // Check if assignment already exists
                if (await db.MemberLevelDepartments.AnyAsync(mld => mld.MemberId == memberId && mld.LevelDepartmentId == levelDepartmentId))
                    return Results.BadRequest("Role already assigned to member");

                var assignment = new MemberLevelDepartment
                {
                    MemberId = memberId,
                    LevelDepartmentId = levelDepartmentId
                };

                db.MemberLevelDepartments.Add(assignment);
                await db.SaveChangesAsync();
                return Results.Created($"/api/members/{memberId}/roles", assignment);
            });

            // Remove role from member
            app.MapDelete("/api/members/{memberId}/roles/{levelDepartmentId}", async (int memberId, int levelDepartmentId, AmsaDbContext db) =>
            {
                var assignment = await db.MemberLevelDepartments
                    .FirstOrDefaultAsync(mld => mld.MemberId == memberId && mld.LevelDepartmentId == levelDepartmentId);
                
                if (assignment == null) return Results.NotFound("Role assignment not found");

                db.MemberLevelDepartments.Remove(assignment);
                await db.SaveChangesAsync();
                return Results.Ok(new { Message = "Role removed successfully" });
            });

            // Get member roles
            app.MapGet("/api/members/{memberId}/roles", async (int memberId, AmsaDbContext db) =>
            {
                var roles = await db.MemberLevelDepartments
                    .Include(mld => mld.LevelDepartment)
                        .ThenInclude(ld => ld.Department)
                    .Include(mld => mld.LevelDepartment)
                        .ThenInclude(ld => ld.Level)
                    .Where(mld => mld.MemberId == memberId)
                    .Select(mld => new
                    {
                        mld.MemberLevelDepartmentId,
                        Department = mld.LevelDepartment.Department.DepartmentName,
                        Level = mld.LevelDepartment.Level.LevelType
                    })
                    .ToListAsync();
                
                return Results.Ok(roles);
            });

            // =========================== STATISTICS ENDPOINTS ===========================
            
            // Get dashboard statistics
            app.MapGet("/api/stats/dashboard", async (AmsaDbContext db) =>
            {
                var stats = await Task.FromResult(new
                {
                    TotalMembers = await db.Members.CountAsync(),
                    TotalUnits = await db.Units.CountAsync(),
                    TotalDepartments = await db.Departments.CountAsync(),
                    TotalStates = await db.States.CountAsync(),
                    TotalNationals = await db.Nationals.CountAsync(),
                    ExcoMembers = await db.MemberLevelDepartments.CountAsync(),
                    RecentMembers = await db.Members.OrderByDescending(m => m.MemberId).Take(5)
                        .Select(m => new { m.FirstName, m.LastName, m.Mkanid }).ToListAsync()
                });
                
                return Results.Ok(stats);
            });

            // Get unit statistics
            app.MapGet("/api/stats/units", async (AmsaDbContext db) =>
            {
                var unitStats = await db.Units
                    .Include(u => u.State)
                    .Select(u => new
                    {
                        u.UnitId, u.UnitName, State = u.State.StateName,
                        MemberCount = u.Members.Count(),
                        ExcoCount = u.Members.SelectMany(m => m.MemberLevelDepartments).Count()
                    })
                    .OrderByDescending(u => u.MemberCount)
                    .ToListAsync();
                
                return Results.Ok(unitStats);
            });

            // Get department statistics
            app.MapGet("/api/stats/departments", async (AmsaDbContext db) =>
            {
                var deptStats = await db.Departments
                    .Select(d => new
                    {
                        d.DepartmentId, d.DepartmentName,
                        MemberCount = d.LevelDepartments.SelectMany(ld => ld.MemberLevelDepartments).Count()
                    })
                    .OrderByDescending(d => d.MemberCount)
                    .ToListAsync();
                
                return Results.Ok(deptStats);
            });

            // =========================== BULK OPERATIONS ===========================
            
            // Bulk delete member roles
            app.MapDelete("/api/members/{memberId}/roles", async (int memberId, AmsaDbContext db) =>
            {
                var roles = await db.MemberLevelDepartments.Where(mld => mld.MemberId == memberId).ToListAsync();
                if (!roles.Any()) return Results.NotFound("No roles found for member");

                db.MemberLevelDepartments.RemoveRange(roles);
                await db.SaveChangesAsync();
                return Results.Ok(new { Message = $"Removed {roles.Count} roles from member" });
            });

            // Clear all EXCO assignments
            app.MapDelete("/api/exco/clear", async (AmsaDbContext db) =>
            {
                var excoAssignments = await db.MemberLevelDepartments.ToListAsync();
                db.MemberLevelDepartments.RemoveRange(excoAssignments);
                await db.SaveChangesAsync();
                return Results.Ok(new { Message = $"Cleared {excoAssignments.Count} EXCO assignments" });
            });

            // =========================== IMPORT ENDPOINTS ===========================
            
            // Import EXCO members from CSV
            app.MapGet("/api/import/exco", async (AmsaDbContext dbContext) =>
            {
                try
                {
                    var importer = new ExcoImporter(dbContext);
                    var unmatchedRecords = await importer.ImportExcoRecords();
                    
                    if (unmatchedRecords.Any())
                    {
                        return Results.Ok(new { 
                            Message = "Import completed with unmatched records.", 
                            UnmatchedRecords = unmatchedRecords,
                            UnmatchedCount = unmatchedRecords.Count
                        });
                    }
                    
                    return Results.Ok(new { Message = "Import completed successfully." });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Import failed: {ex.Message}");
                }
            });

            // Upload and import CSV file
            app.MapPost("/api/import/exco/upload", async (IFormFile csvFile, AmsaDbContext dbContext) =>
            {
                try
                {
                    if (csvFile == null || csvFile.Length == 0)
                        return Results.BadRequest("No file uploaded");

                    if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        return Results.BadRequest("Only CSV files are allowed");
                    
                    var importer = new ExcoImporter(dbContext);
                    var tempPath = Path.GetTempFileName();
                    
                    using (var stream = new FileStream(tempPath, FileMode.Create))
                    {
                        await csvFile.CopyToAsync(stream);
                    }

                    var unmatchedRecords = await importer.ImportExcoRecords(tempPath);
                    File.Delete(tempPath);
                    
                    if (unmatchedRecords.Any())
                    {
                        return Results.Ok(new { 
                            Message = "Import completed with unmatched records.", 
                            UnmatchedRecords = unmatchedRecords,
                            UnmatchedCount = unmatchedRecords.Count
                        });
                    }
                    
                    return Results.Ok(new { Message = "Import completed successfully." });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Import failed: {ex.Message}");
                }
            });

            // Test CSV file exists
            app.MapGet("/api/import/test", () =>
            {
                var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "excos_list_updated.csv");
                return Results.Ok(new 
                { 
                    CsvFileExists = File.Exists(csvPath),
                    CsvPath = csvPath,
                    CurrentDirectory = Directory.GetCurrentDirectory()
                });
            });
        }
    }
}