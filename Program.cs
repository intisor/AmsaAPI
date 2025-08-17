using AmsaAPI;
using AmsaAPI.Data;
using AmsaNigeriaApi.Endpoints;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AmsaDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JSON serialization to handle reference cycles
builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
	options.SerializerOptions.WriteIndented = true;
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable static files (for test.html)
app.UseStaticFiles();

app.MapGet("/", () => "Welcome to the AMSA API! Visit /test.html to test the endpoints.");


// Add basic member endpoints with DTOs to avoid circular references
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

// This will add the /api/import/exco endpoint
app.MapMemberEndpoints();
app.Run();
