using FastEndpoints;
using FastEndpoints.Swagger;
using AmsaAPI.Data;
using AmsaAPI.Endpoints;
using AmsaAPI.FastEndpoints;
using AmsaAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();

// Configure DbContext with connection string from appsettings
builder.Services.AddDbContext<AmsaDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null
        )
    )
);

builder.Services.AddFastEndpoints();


// Register import services for minimal API endpoints
builder.Services.AddScoped<CsvValidationHelper>();
builder.Services.AddScoped<MemberImporter>();

// Register authentication/token services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AppRegistrationService>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = builder.Configuration["Jwt:SecretKey"];

if (string.IsNullOrEmpty(secretKey))
    throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long. Check user-secrets or appsettings.");

if (secretKey.Length < 32)
	throw new InvalidOperationException(
		$"JWT SecretKey must be at least 32 characters long (current length: {secretKey.Length}). " +
		"Update it using: dotnet user-secrets set \"Jwt:SecretKey\" \"your-longer-secret-key-here\"");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
		options.SaveToken = true;
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = jwtSettings["Issuer"],

			ValidateAudience = true,
			ValidAudiences = ["ReportingApp", "EventsApp", "PaymentApp"],

			ValidateLifetime = true,
			ClockSkew = TimeSpan.FromMinutes(1),

			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
		};
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AmsaDbContext>();
    db.Database.Migrate();

    // Seed ReportingApp if missing
    var reportingApp = await db.AppRegistrations.FirstOrDefaultAsync(a => a.AppId == "ReportingApp");
    if (reportingApp is null)
    {
        db.AppRegistrations.Add(new AppRegistration
        {
            AppId = "ReportingApp",
            AppName = "AMSA Reporting Application",
            // Replace with a securely stored hash of the app secret in production
            AppSecretHash = "test-secret-123-hash",
            AllowedScopes = "[\"read:members\", \"read:statistics\", \"read:organization\"]",
            TokenExpirationHours = 2,
            IsActive = true
        });
        await db.SaveChangesAsync();
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Enable Swagger UI only in Development
    app.UseSwaggerGen();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication & Authorization middleware (order matters!)
app.UseAuthentication();
app.UseAuthorization();

// Map Razor Pages
app.MapRazorPages();

app.UseFastEndpoints();

// Welcome message with API information
app.MapGet("/", () => "Welcome to the AMSA Nigeria API! " +
    "FastEndpoints: /api/* | Minimal API: /api/minimal/* | Swagger: /swagger | Test: /test.html");

// Map all minimal API endpoints
app.MapMemberEndpoints();
app.MapOrganizationEndpoints();
app.MapDepartmentEndpoints();
app.MapStatisticsEndpoints();
app.MapImportEndpoints();

app.Run();
