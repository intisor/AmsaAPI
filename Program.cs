using AmsaAPI.Data;
using AmsaAPI.Endpoints;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<AmsaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.WriteIndented = true;
});

// Add FastEndpoints
builder.Services.AddFastEndpoints();

var app = builder.Build();

// Configure pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseFastEndpoints();

// Welcome message
app.MapGet("/", () => "Welcome to the AMSA Nigeria API! " +
    "FastEndpoints: /api/* | Minimal API: /api/minimal/* | Test: /test.html");

// Map all minimal API endpoints
app.MapMemberEndpoints();
app.MapOrganizationEndpoints();
app.MapDepartmentEndpoints();
app.MapStatisticsEndpoints();
app.MapImportEndpoints();

app.Run();
