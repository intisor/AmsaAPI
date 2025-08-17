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

app.MapMemberEndpoints();
app.Run();
