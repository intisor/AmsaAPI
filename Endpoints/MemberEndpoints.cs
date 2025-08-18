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
                    .Include(m => m.MemberLevelDepartments)
                        .ThenInclude(mld => mld.LevelDepartment)
                            .ThenInclude(ld => ld.Level)
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
                            LevelType = mld.LevelDepartment.Level.LevelType,
                            Scope = mld.LevelDepartment.Level.NationalId != null ? "National" :
                                   mld.LevelDepartment.Level.StateId != null ? "State" : 
                                   mld.LevelDepartment.Level.UnitId != null ? "Unit" : "Unknown"
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
                    .Include(m => m.MemberLevelDepartments)
                        .ThenInclude(mld => mld.LevelDepartment)
                            .ThenInclude(ld => ld.Level)
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
                            LevelType = mld.LevelDepartment.Level.LevelType,
                            Scope = mld.LevelDepartment.Level.NationalId != null ? "National" :
                                   mld.LevelDepartment.Level.StateId != null ? "State" : 
                                   mld.LevelDepartment.Level.UnitId != null ? "Unit" : "Unknown"
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
                    .Where(m => m.MemberLevelDepartments.Any(mld => mld.LevelDepartment.DepartmentId == departmentId))
                    .Select(m => new { m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid })
                    .ToListAsync();
                
                return Results.Ok(members);
            });

            // Get member by MKAN ID
            app.MapGet("/api/members/mkan/{mkanId}", async (int mkanId, AmsaDbContext db) =>
            {
                var member = await db.Members
                    .Include(m => m.Unit)
                        .ThenInclude(u => u.State)
                            .ThenInclude(s => s.National)
                    .Include(m => m.MemberLevelDepartments)
                        .ThenInclude(mld => mld.LevelDepartment)
                            .ThenInclude(ld => ld.Department)
                    .Include(m => m.MemberLevelDepartments)
                        .ThenInclude(mld => mld.LevelDepartment)
                            .ThenInclude(ld => ld.Level)
                    .Where(m => m.Mkanid == mkanId)
                    .Select(m => new
                    {
                        m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid,
                        Unit = new { 
                            m.Unit.UnitId, 
                            m.Unit.UnitName, 
                            State = new {
                                m.Unit.State.StateId,
                                m.Unit.State.StateName,
                                National = new {
                                    m.Unit.State.National.NationalId,
                                    m.Unit.State.National.NationalName
                                }
                            }
                        },
                        Roles = m.MemberLevelDepartments.Select(mld => new {
                            mld.LevelDepartment.Department.DepartmentName,
                            LevelType = mld.LevelDepartment.Level.LevelType,
                            Scope = mld.LevelDepartment.Level.NationalId != null ? "National" :
                                   mld.LevelDepartment.Level.StateId != null ? "State" : 
                                   mld.LevelDepartment.Level.UnitId != null ? "Unit" : "Unknown"
                        })
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

                    // Validate UnitId exists
                    if (!await db.Units.AnyAsync(u => u.UnitId == member.UnitId))
                        return Results.BadRequest($"Unit with ID {member.UnitId} does not exist");

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

                // Validate UnitId exists if changed
                if (member.UnitId != updatedMember.UnitId && !await db.Units.AnyAsync(u => u.UnitId == updatedMember.UnitId))
                    return Results.BadRequest($"Unit with ID {updatedMember.UnitId} does not exist");

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
                        d.DepartmentName,
                        MemberCount = d.LevelDepartments.SelectMany(ld => ld.MemberLevelDepartments).Count()
                    })
                    .ToListAsync();
                
                return Results.Ok(departments);
            });
            
            // Create department
            app.MapPost("/api/departments", async (Department department, AmsaDbContext db) =>
            {
                try
                {
                    // Check if department name already exists
                    if (await db.Departments.AnyAsync(d => d.DepartmentName == department.DepartmentName))
                        return Results.BadRequest($"Department '{department.DepartmentName}' already exists");

                    db.Departments.Add(department);
                    await db.SaveChangesAsync();
                    return Results.Created($"/api/departments/{department.DepartmentId}", department);
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Failed to create department: {ex.Message}");
                }
            });

            // Update department
            app.MapPut("/api/departments/{id}", async (int id, Department updatedDept, AmsaDbContext db) =>
            {
                var department = await db.Departments.FindAsync(id);
                if (department == null) return Results.NotFound();

                // Check if new name already exists (excluding current department)
                if (await db.Departments.AnyAsync(d => d.DepartmentName == updatedDept.DepartmentName && d.DepartmentId != id))
                    return Results.BadRequest($"Department '{updatedDept.DepartmentName}' already exists");

                department.DepartmentName = updatedDept.DepartmentName;
                await db.SaveChangesAsync();
                return Results.Ok(department);
            });

            // Delete department
            app.MapDelete("/api/departments/{id}", async (int id, AmsaDbContext db) =>
            {
                var department = await db.Departments.FindAsync(id);
                if (department == null) return Results.NotFound();

                // Check if department has associated level departments
                if (await db.LevelDepartments.AnyAsync(ld => ld.DepartmentId == id))
                    return Results.BadRequest("Cannot delete department with existing level assignments");

                db.Departments.Remove(department);
                await db.SaveChangesAsync();
                return Results.Ok(new { Message = $"Department {department.DepartmentName} deleted successfully" });
            });

            // Get department by ID
            app.MapGet("/api/departments/{id}", async (int id, AmsaDbContext db) =>
            {
                var department = await db.Departments
                    .Where(d => d.DepartmentId == id)
                    .Select(d => new
                    {
                        d.DepartmentId,
                        d.DepartmentName,
                        Levels = d.LevelDepartments.Select(ld => new
                        {
                            ld.LevelDepartmentId,
                            ld.Level.LevelType,
                            Scope = ld.Level.NationalId != null ? "National" :
                                   ld.Level.StateId != null ? "State" : 
                                   ld.Level.UnitId != null ? "Unit" : "Unknown",
                            MemberCount = ld.MemberLevelDepartments.Count(),
                            Members = ld.MemberLevelDepartments.Select(mld => new { 
                                mld.Member.MemberId,
                                mld.Member.FirstName, 
                                mld.Member.LastName, 
                                mld.Member.Mkanid 
                            })
                        })
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
                        u.StateId,
                        State = new
                        {
                            u.State.StateId,
                            u.State.StateName,
                            National = new
                            {
                                u.State.National.NationalId,
                                u.State.National.NationalName
                            }
                        },
                        MemberCount = u.Members.Count()
                    })
                    .ToListAsync();
                
                return Results.Ok(units);
            });
            
            // Create unit
            app.MapPost("/api/units", async (Unit unit, AmsaDbContext db) =>
            {
                try
                {
                    // Validate StateId exists
                    if (!await db.States.AnyAsync(s => s.StateId == unit.StateId))
                        return Results.BadRequest($"State with ID {unit.StateId} does not exist");

                    db.Units.Add(unit);
                    await db.SaveChangesAsync();
                    return Results.Created($"/api/units/{unit.UnitId}", unit);
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Failed to create unit: {ex.Message}");
                }
            });

            // Update unit
            app.MapPut("/api/units/{id}", async (int id, Unit updatedUnit, AmsaDbContext db) =>
            {
                var unit = await db.Units.FindAsync(id);
                if (unit == null) return Results.NotFound();

                // Validate StateId exists if changed
                if (unit.StateId != updatedUnit.StateId && !await db.States.AnyAsync(s => s.StateId == updatedUnit.StateId))
                    return Results.BadRequest($"State with ID {updatedUnit.StateId} does not exist");

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

                // Check if unit has members
                if (await db.Members.AnyAsync(m => m.UnitId == id))
                    return Results.BadRequest("Cannot delete unit with existing members");

                db.Units.Remove(unit);
                await db.SaveChangesAsync();
                return Results.Ok(new { Message = $"Unit {unit.UnitName} deleted successfully" });
            });

            // Get unit by ID with members
            app.MapGet("/api/units/{id}", async (int id, AmsaDbContext db) =>
            {
                var unit = await db.Units
                    .Include(u => u.State)
                        .ThenInclude(s => s.National)
                    .Where(u => u.UnitId == id)
                    .Select(u => new
                    {
                        u.UnitId, 
                        u.UnitName,
                        State = new { 
                            u.State.StateId, 
                            u.State.StateName, 
                            National = new {
                                u.State.National.NationalId,
                                u.State.National.NationalName
                            }
                        },
                        MemberCount = u.Members.Count(),
                        Members = u.Members.Select(m => new { 
                            m.MemberId,
                            m.FirstName, 
                            m.LastName, 
                            m.Mkanid,
                            m.Email,
                            m.Phone
                        }),
                        ExcoRoles = u.Levels.SelectMany(l => l.LevelDepartments)
                            .SelectMany(ld => ld.MemberLevelDepartments)
                            .Select(mld => new {
                                mld.Member.FirstName,
                                mld.Member.LastName,
                                mld.Member.Mkanid,
                                Department = mld.LevelDepartment.Department.DepartmentName,
                                Level = mld.LevelDepartment.Level.LevelType
                            })
                    })
                    .FirstOrDefaultAsync();
                
                return unit != null ? Results.Ok(unit) : Results.NotFound();
            });

            // Get units by state
            app.MapGet("/api/units/state/{stateId}", async (int stateId, AmsaDbContext db) =>
            {
                var units = await db.Units
                    .Where(u => u.StateId == stateId)
                    .Select(u => new { 
                        u.UnitId, 
                        u.UnitName, 
                        MemberCount = u.Members.Count(),
                        ExcoCount = u.Levels.SelectMany(l => l.LevelDepartments)
                            .SelectMany(ld => ld.MemberLevelDepartments).Count()
                    })
                    .ToListAsync();
                
                return Results.Ok(units);
            });

            // =========================== STATE ENDPOINTS ===========================
            
            // Get all states
            app.MapGet("/api/states", async (AmsaDbContext db) =>
            {
                var states = await db.States
                    .Include(s => s.National)
                    .Select(s => new { 
                        s.StateId, 
                        s.StateName, 
                        s.NationalId,
                        National = s.National.NationalName, 
                        UnitCount = s.Units.Count(),
                        MemberCount = s.Units.SelectMany(u => u.Members).Count(),
                        ExcoCount = s.Levels.SelectMany(l => l.LevelDepartments)
                            .SelectMany(ld => ld.MemberLevelDepartments).Count()
                    })
                    .ToListAsync();
                
                return Results.Ok(states);
            });

            // Get state by ID
            app.MapGet("/api/states/{id}", async (int id, AmsaDbContext db) =>
            {
                var state = await db.States
                    .Include(s => s.National)
                    .Where(s => s.StateId == id)
                    .Select(s => new
                    {
                        s.StateId, 
                        s.StateName,
                        National = new { 
                            s.National.NationalId, 
                            s.National.NationalName 
                        },
                        UnitCount = s.Units.Count(),
                        MemberCount = s.Units.SelectMany(u => u.Members).Count(),
                        Units = s.Units.Select(u => new { 
                            u.UnitId, 
                            u.UnitName,
                            MemberCount = u.Members.Count()
                        }),
                        ExcoRoles = s.Levels.SelectMany(l => l.LevelDepartments)
                            .SelectMany(ld => ld.MemberLevelDepartments)
                            .Select(mld => new {
                                mld.Member.FirstName,
                                mld.Member.LastName,
                                mld.Member.Mkanid,
                                Department = mld.LevelDepartment.Department.DepartmentName,
                                Level = mld.LevelDepartment.Level.LevelType
                            })
                    })
                    .FirstOrDefaultAsync();
                
                return state != null ? Results.Ok(state) : Results.NotFound();
            });

            // Create state
            app.MapPost("/api/states", async (State state, AmsaDbContext db) =>
            {
                try
                {
                    // Validate NationalId exists
                    if (!await db.Nationals.AnyAsync(n => n.NationalId == state.NationalId))
                        return Results.BadRequest($"National with ID {state.NationalId} does not exist");

                    // Check if state name already exists
                    if (await db.States.AnyAsync(s => s.StateName == state.StateName))
                        return Results.BadRequest($"State '{state.StateName}' already exists");

                    db.States.Add(state);
                    await db.SaveChangesAsync();
                    return Results.Created($"/api/states/{state.StateId}", state);
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Failed to create state: {ex.Message}");
                }
            });

            // =========================== NATIONAL ENDPOINTS ===========================
            
            // Get all nationals
            app.MapGet("/api/nationals", async (AmsaDbContext db) =>
            {
                var nationals = await db.Nationals
                    .Select(n => new { 
                        n.NationalId, 
                        n.NationalName, 
                        StateCount = n.States.Count(),
                        UnitCount = n.States.SelectMany(s => s.Units).Count(),
                        MemberCount = n.States.SelectMany(s => s.Units).SelectMany(u => u.Members).Count(),
                        ExcoCount = n.Levels.SelectMany(l => l.LevelDepartments)
                            .SelectMany(ld => ld.MemberLevelDepartments).Count()
                    })
                    .ToListAsync();
                
                return Results.Ok(nationals);
            });

            // Get national by ID
            app.MapGet("/api/nationals/{id}", async (int id, AmsaDbContext db) =>
            {
                var national = await db.Nationals
                    .Where(n => n.NationalId == id)
                    .Select(n => new
                    {
                        n.NationalId, 
                        n.NationalName,
                        StateCount = n.States.Count(),
                        UnitCount = n.States.SelectMany(s => s.Units).Count(),
                        MemberCount = n.States.SelectMany(s => s.Units).SelectMany(u => u.Members).Count(),
                        States = n.States.Select(s => new { 
                            s.StateId, 
                            s.StateName,
                            UnitCount = s.Units.Count(),
                            MemberCount = s.Units.SelectMany(u => u.Members).Count()
                        }),
                        NationalExcoRoles = n.Levels.SelectMany(l => l.LevelDepartments)
                            .SelectMany(ld => ld.MemberLevelDepartments)
                            .Select(mld => new {
                                mld.Member.FirstName,
                                mld.Member.LastName,
                                mld.Member.Mkanid,
                                Department = mld.LevelDepartment.Department.DepartmentName,
                                Level = mld.LevelDepartment.Level.LevelType
                            })
                    })
                    .FirstOrDefaultAsync();
                
                return national != null ? Results.Ok(national) : Results.NotFound();
            });

            // =========================== LEVEL ENDPOINTS ===========================
            
            // Get all levels
            app.MapGet("/api/levels", async (AmsaDbContext db) =>
            {
                var levels = await db.Levels
                    .Include(l => l.National)
                    .Include(l => l.State)
                    .Include(l => l.Unit)
                    .Select(l => new { 
                        l.LevelId, 
                        l.LevelType,
                        Scope = l.NationalId != null ? "National" :
                               l.StateId != null ? "State" : 
                               l.UnitId != null ? "Unit" : "Unknown",
                        ScopeId = l.NationalId ?? l.StateId ?? l.UnitId,
                        ScopeName = l.National != null ? l.National.NationalName :
                                   l.State != null ? l.State.StateName :
                                   l.Unit != null ? l.Unit.UnitName : "Unknown",
                        DepartmentCount = l.LevelDepartments.Count(),
                        MemberCount = l.LevelDepartments.SelectMany(ld => ld.MemberLevelDepartments).Count()
                    })
                    .ToListAsync();
                
                return Results.Ok(levels);
            });

            // Get level departments
            app.MapGet("/api/levels/{id}/departments", async (int id, AmsaDbContext db) =>
            {
                var departments = await db.LevelDepartments
                    .Include(ld => ld.Department)
                    .Include(ld => ld.Level)
                    .Where(ld => ld.LevelId == id)
                    .Select(ld => new
                    {
                        ld.LevelDepartmentId,
                        Department = new { 
                            ld.Department.DepartmentId, 
                            ld.Department.DepartmentName 
                        },
                        Level = new {
                            ld.Level.LevelId,
                            ld.Level.LevelType,
                            Scope = ld.Level.NationalId != null ? "National" :
                                   ld.Level.StateId != null ? "State" : 
                                   ld.Level.UnitId != null ? "Unit" : "Unknown"
                        },
                        MemberCount = ld.MemberLevelDepartments.Count(),
                        Members = ld.MemberLevelDepartments.Select(mld => new {
                            mld.Member.MemberId,
                            mld.Member.FirstName,
                            mld.Member.LastName,
                            mld.Member.Mkanid
                        })
                    })
                    .ToListAsync();
                
                return Results.Ok(departments);
            });

            // =========================== ROLE ASSIGNMENT ENDPOINTS ===========================
            
            // Assign role to member
            app.MapPost("/api/members/{memberId}/roles/{levelDepartmentId}", async (int memberId, int levelDepartmentId, AmsaDbContext db) =>
            {
                // Validate member exists
                if (!await db.Members.AnyAsync(m => m.MemberId == memberId))
                    return Results.BadRequest($"Member with ID {memberId} does not exist");

                // Validate level department exists
                if (!await db.LevelDepartments.AnyAsync(ld => ld.LevelDepartmentId == levelDepartmentId))
                    return Results.BadRequest($"Level Department with ID {levelDepartmentId} does not exist");

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
                        mld.LevelDepartmentId,
                        Department = mld.LevelDepartment.Department.DepartmentName,
                        Level = mld.LevelDepartment.Level.LevelType,
                        Scope = mld.LevelDepartment.Level.NationalId != null ? "National" :
                               mld.LevelDepartment.Level.StateId != null ? "State" : 
                               mld.LevelDepartment.Level.UnitId != null ? "Unit" : "Unknown"
                    })
                    .ToListAsync();
                
                return Results.Ok(roles);
            });

            // =========================== STATISTICS ENDPOINTS ===========================
            
            // Get dashboard statistics
            app.MapGet("/api/stats/dashboard", async (AmsaDbContext db) =>
            {
                var stats = new
                {
                    TotalMembers = await db.Members.CountAsync(),
                    TotalUnits = await db.Units.CountAsync(),
                    TotalDepartments = await db.Departments.CountAsync(),
                    TotalStates = await db.States.CountAsync(),
                    TotalNationals = await db.Nationals.CountAsync(),
                    TotalLevels = await db.Levels.CountAsync(),
                    ExcoMembers = await db.MemberLevelDepartments.CountAsync(),
                    RecentMembers = await db.Members.OrderByDescending(m => m.MemberId).Take(5)
                        .Select(m => new { m.FirstName, m.LastName, m.Mkanid }).ToListAsync(),
                    NationalExcoCount = await db.Levels.Where(l => l.NationalId != null)
                        .SelectMany(l => l.LevelDepartments)
                        .SelectMany(ld => ld.MemberLevelDepartments)
                        .CountAsync(),
                    StateExcoCount = await db.Levels.Where(l => l.StateId != null)
                        .SelectMany(l => l.LevelDepartments)
                        .SelectMany(ld => ld.MemberLevelDepartments)
                        .CountAsync(),
                    UnitExcoCount = await db.Levels.Where(l => l.UnitId != null)
                        .SelectMany(l => l.LevelDepartments)
                        .SelectMany(ld => ld.MemberLevelDepartments)
                        .CountAsync()
                };
                
                return Results.Ok(stats);
            });

            // Get unit statistics
            app.MapGet("/api/stats/units", async (AmsaDbContext db) =>
            {
                var unitStats = await db.Units
                    .Include(u => u.State)
                    .Select(u => new
                    {
                        u.UnitId, 
                        u.UnitName, 
                        State = u.State.StateName,
                        MemberCount = u.Members.Count(),
                        ExcoCount = u.Levels.SelectMany(l => l.LevelDepartments)
                            .SelectMany(ld => ld.MemberLevelDepartments).Count()
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
                        d.DepartmentId, 
                        d.DepartmentName,
                        MemberCount = d.LevelDepartments.SelectMany(ld => ld.MemberLevelDepartments).Count(),
                        NationalCount = d.LevelDepartments.Where(ld => ld.Level.NationalId != null)
                            .SelectMany(ld => ld.MemberLevelDepartments).Count(),
                        StateCount = d.LevelDepartments.Where(ld => ld.Level.StateId != null)
                            .SelectMany(ld => ld.MemberLevelDepartments).Count(),
                        UnitCount = d.LevelDepartments.Where(ld => ld.Level.UnitId != null)
                            .SelectMany(ld => ld.MemberLevelDepartments).Count()
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

            // =========================== ADVANCED QUERY ENDPOINTS ===========================
            
            // Get members by level type (e.g., President, Secretary, etc.)
            app.MapGet("/api/members/level/{levelType}", async (string levelType, AmsaDbContext db) =>
            {
                var members = await db.Members
                    .Where(m => m.MemberLevelDepartments.Any(mld => mld.LevelDepartment.Level.LevelType == levelType))
                    .Select(m => new {
                        m.MemberId,
                        m.FirstName,
                        m.LastName,
                        m.Mkanid,
                        Unit = new {
                            m.Unit.UnitName,
                            State = m.Unit.State.StateName
                        },
                        Departments = m.MemberLevelDepartments
                            .Where(mld => mld.LevelDepartment.Level.LevelType == levelType)
                            .Select(mld => new {
                                mld.LevelDepartment.Department.DepartmentName,
                                Scope = mld.LevelDepartment.Level.NationalId != null ? "National" :
                                       mld.LevelDepartment.Level.StateId != null ? "State" : 
                                       mld.LevelDepartment.Level.UnitId != null ? "Unit" : "Unknown"
                            })
                    })
                    .ToListAsync();
                
                return Results.Ok(members);
            });

            // Get level departments by scope (National, State, Unit)
            app.MapGet("/api/leveldepartments/scope/{scope}", async (string scope, AmsaDbContext db) =>
            {
                var query = db.LevelDepartments.AsQueryable();

                query = scope.ToLower() switch
                {
                    "national" => query.Where(ld => ld.Level.NationalId != null),
                    "state" => query.Where(ld => ld.Level.StateId != null),
                    "unit" => query.Where(ld => ld.Level.UnitId != null),
                    _ => query
                };

                var levelDepartments = await query
                    .Include(ld => ld.Level)
                    .Include(ld => ld.Department)
                    .Select(ld => new
                    {
                        ld.LevelDepartmentId,
                        Department = ld.Department.DepartmentName,
                        Level = ld.Level.LevelType,
                        Scope = ld.Level.NationalId != null ? "National" :
                               ld.Level.StateId != null ? "State" : 
                               ld.Level.UnitId != null ? "Unit" : "Unknown",
                        MemberCount = ld.MemberLevelDepartments.Count()
                    })
                    .ToListAsync();

                return Results.Ok(levelDepartments);
            });

            // Get hierarchy overview
            app.MapGet("/api/hierarchy", async (AmsaDbContext db) =>
            {
                var hierarchy = await db.Nationals
                    .Select(n => new
                    {
                        National = new { n.NationalId, n.NationalName },
                        States = n.States.Select(s => new
                        {
                            State = new { s.StateId, s.StateName },
                            Units = s.Units.Select(u => new
                            {
                                Unit = new { u.UnitId, u.UnitName },
                                MemberCount = u.Members.Count()
                            })
                        })
                    })
                    .ToListAsync();

                return Results.Ok(hierarchy);
            });

            // =========================== ADDITIONAL USEFUL ENDPOINTS ===========================
            
            // Get all level departments
            app.MapGet("/api/leveldepartments", async (AmsaDbContext db) =>
            {
                var levelDepartments = await db.LevelDepartments
                    .Include(ld => ld.Level)
                    .Include(ld => ld.Department)
                    .Select(ld => new
                    {
                        ld.LevelDepartmentId,
                        ld.LevelId,
                        ld.DepartmentId,
                        Department = ld.Department.DepartmentName,
                        Level = ld.Level.LevelType,
                        Scope = ld.Level.NationalId != null ? "National" :
                               ld.Level.StateId != null ? "State" : 
                               ld.Level.UnitId != null ? "Unit" : "Unknown",
                        ScopeId = ld.Level.NationalId ?? ld.Level.StateId ?? ld.Level.UnitId,
                        MemberCount = ld.MemberLevelDepartments.Count()
                    })
                    .ToListAsync();

                return Results.Ok(levelDepartments);
            });

            // Create level department
            app.MapPost("/api/leveldepartments", async (LevelDepartment levelDepartment, AmsaDbContext db) =>
            {
                try
                {
                    // Validate LevelId exists
                    if (!await db.Levels.AnyAsync(l => l.LevelId == levelDepartment.LevelId))
                        return Results.BadRequest($"Level with ID {levelDepartment.LevelId} does not exist");

                    // Validate DepartmentId exists
                    if (!await db.Departments.AnyAsync(d => d.DepartmentId == levelDepartment.DepartmentId))
                        return Results.BadRequest($"Department with ID {levelDepartment.DepartmentId} does not exist");

                    // Check if combination already exists
                    if (await db.LevelDepartments.AnyAsync(ld => ld.LevelId == levelDepartment.LevelId && ld.DepartmentId == levelDepartment.DepartmentId))
                        return Results.BadRequest("Level-Department combination already exists");

                    db.LevelDepartments.Add(levelDepartment);
                    await db.SaveChangesAsync();
                    return Results.Created($"/api/leveldepartments/{levelDepartment.LevelDepartmentId}", levelDepartment);
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Failed to create level department: {ex.Message}");
                }
            });

            // Delete level department
            app.MapDelete("/api/leveldepartments/{id}", async (int id, AmsaDbContext db) =>
            {
                var levelDepartment = await db.LevelDepartments.FindAsync(id);
                if (levelDepartment == null) return Results.NotFound();

                // Check if there are member assignments
                if (await db.MemberLevelDepartments.AnyAsync(mld => mld.LevelDepartmentId == id))
                    return Results.BadRequest("Cannot delete level department with existing member assignments");

                db.LevelDepartments.Remove(levelDepartment);
                await db.SaveChangesAsync();
                return Results.Ok(new { Message = "Level department deleted successfully" });
            });

            // Create level
            app.MapPost("/api/levels", async (Level level, AmsaDbContext db) =>
            {
                try
                {
                    // Validate that only one scope is set
                    var scopeCount = (level.NationalId.HasValue ? 1 : 0) + 
                                   (level.StateId.HasValue ? 1 : 0) + 
                                   (level.UnitId.HasValue ? 1 : 0);
                    
                    if (scopeCount != 1)
                        return Results.BadRequest("Level must be associated with exactly one scope (National, State, or Unit)");

                    // Validate the associated scope exists
                    if (level.NationalId.HasValue && !await db.Nationals.AnyAsync(n => n.NationalId == level.NationalId))
                        return Results.BadRequest($"National with ID {level.NationalId} does not exist");

                    if (level.StateId.HasValue && !await db.States.AnyAsync(s => s.StateId == level.StateId))
                        return Results.BadRequest($"State with ID {level.StateId} does not exist");

                    if (level.UnitId.HasValue && !await db.Units.AnyAsync(u => u.UnitId == level.UnitId))
                        return Results.BadRequest($"Unit with ID {level.UnitId} does not exist");

                    // Check if level type already exists for this scope
                    var existingLevel = await db.Levels.AnyAsync(l => 
                        l.LevelType == level.LevelType &&
                        l.NationalId == level.NationalId &&
                        l.StateId == level.StateId &&
                        l.UnitId == level.UnitId);

                    if (existingLevel)
                        return Results.BadRequest($"Level type '{level.LevelType}' already exists for this scope");

                    db.Levels.Add(level);
                    await db.SaveChangesAsync();
                    return Results.Created($"/api/levels/{level.LevelId}", level);
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Failed to create level: {ex.Message}");
                }
            });

            // Get members with their complete organizational structure
            app.MapGet("/api/members/complete", async (AmsaDbContext db) =>
            {
                var members = await db.Members
                    .Include(m => m.Unit)
                        .ThenInclude(u => u.State)
                            .ThenInclude(s => s.National)
                    .Include(m => m.MemberLevelDepartments)
                        .ThenInclude(mld => mld.LevelDepartment)
                            .ThenInclude(ld => ld.Department)
                    .Include(m => m.MemberLevelDepartments)
                        .ThenInclude(mld => mld.LevelDepartment)
                            .ThenInclude(ld => ld.Level)
                    .Select(m => new
                    {
                        Member = new
                        {
                            m.MemberId,
                            m.FirstName,
                            m.LastName,
                            m.Email,
                            m.Phone,
                            m.Mkanid
                        },
                        Organization = new
                        {
                            Unit = new
                            {
                                m.Unit.UnitId,
                                m.Unit.UnitName
                            },
                            State = new
                            {
                                m.Unit.State.StateId,
                                m.Unit.State.StateName
                            },
                            National = new
                            {
                                m.Unit.State.National.NationalId,
                                m.Unit.State.National.NationalName
                            }
                        },
                        ExcoRoles = m.MemberLevelDepartments.Select(mld => new
                        {
                            Department = mld.LevelDepartment.Department.DepartmentName,
                            Level = mld.LevelDepartment.Level.LevelType,
                            Scope = mld.LevelDepartment.Level.NationalId != null ? "National" :
                                   mld.LevelDepartment.Level.StateId != null ? "State" : 
                                   mld.LevelDepartment.Level.UnitId != null ? "Unit" : "Unknown",
                            ScopeName = mld.LevelDepartment.Level.NationalId != null ? m.Unit.State.National.NationalName :
                                       mld.LevelDepartment.Level.StateId != null ? m.Unit.State.StateName :
                                       mld.LevelDepartment.Level.UnitId != null ? m.Unit.UnitName : "Unknown"
                        }),
                        HasExcoRole = m.MemberLevelDepartments.Any()
                    })
                    .ToListAsync();

                return Results.Ok(members);
            });

            // Get organization summary
            app.MapGet("/api/organization/summary", async (AmsaDbContext db) =>
            {
                var summary = new
                {
                    Overview = new
                    {
                        TotalNationals = await db.Nationals.CountAsync(),
                        TotalStates = await db.States.CountAsync(),
                        TotalUnits = await db.Units.CountAsync(),
                        TotalMembers = await db.Members.CountAsync(),
                        TotalDepartments = await db.Departments.CountAsync(),
                        TotalLevels = await db.Levels.CountAsync(),
                        TotalExcoPositions = await db.MemberLevelDepartments.CountAsync()
                    },
                    ExcoBreakdown = new
                    {
                        NationalExco = await db.Levels.Where(l => l.NationalId != null)
                            .SelectMany(l => l.LevelDepartments)
                            .SelectMany(ld => ld.MemberLevelDepartments)
                            .CountAsync(),
                        StateExco = await db.Levels.Where(l => l.StateId != null)
                            .SelectMany(l => l.LevelDepartments)
                            .SelectMany(ld => ld.MemberLevelDepartments)
                            .CountAsync(),
                        UnitExco = await db.Levels.Where(l => l.UnitId != null)
                            .SelectMany(l => l.LevelDepartments)
                            .SelectMany(ld => ld.MemberLevelDepartments)
                            .CountAsync()
                    },
                    TopUnits = await db.Units
                        .Select(u => new
                        {
                            u.UnitName,
                            State = u.State.StateName,
                            MemberCount = u.Members.Count()
                        })
                        .OrderByDescending(u => u.MemberCount)
                        .Take(10)
                        .ToListAsync(),
                    TopDepartments = await db.Departments
                        .Select(d => new
                        {
                            d.DepartmentName,
                            MemberCount = d.LevelDepartments.SelectMany(ld => ld.MemberLevelDepartments).Count()
                        })
                        .OrderByDescending(d => d.MemberCount)
                        .Take(10)
                        .ToListAsync()
                };

                return Results.Ok(summary);
            });
        }
    }
}