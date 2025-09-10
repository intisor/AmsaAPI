using AmsaAPI.Data;
using AmsaAPI.Endpoints;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<AmsaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JSON serialization to handle reference cycles
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.WriteIndented = true;
});

// Add OpenAPI/Swagger for development
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable static files (for test.html and other static content)
app.UseStaticFiles();

// Add CORS if needed for frontend applications
//app.UseCors(policy => policy
//    .AllowAnyOrigin()
//    .AllowAnyMethod()
//    .AllowAnyHeader());

// Welcome message
app.MapGet("/", () => "Welcome to the AMSA Nigeria API! Visit /test.html to test the endpoints.");

// Map all endpoint groups
app.MapMemberEndpoints();
app.MapStatisticsEndpoints();
app.MapImportEndpoints();
app.MapDepartmentEndpoints();
app.MapOrganizationEndpoints();

app.Run();
