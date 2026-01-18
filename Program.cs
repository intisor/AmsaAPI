using FastEndpoints;
using AmsaAPI.Data;
using AmsaAPI.Endpoints;
using AmsaAPI.FastEndpoints;
using AmsaAPI.Services;
using AmsaAPI.DTOs;
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

if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
    throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long. Check user-secrets or appsettings.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            
            ValidateAudience = true,
            ValidAudiences = new[] { "ReportingApp", "EventsApp", "PaymentApp" },
            
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
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
    "FastEndpoints: /api/* | Minimal API: /api/minimal/* | Test: /test.html");

// Map all minimal API endpoints
app.MapMemberEndpoints();
app.MapOrganizationEndpoints();
app.MapDepartmentEndpoints();
app.MapStatisticsEndpoints();
app.MapImportEndpoints();

// Map authentication endpoints
app.MapPost("/api/auth/token", GenerateToken)
    .WithName("Generate Token")
    .WithDescription("Generate JWT token for external app");

app.MapPost("/api/auth/apps", CreateApp)
    .WithName("Create App")
    .RequireAuthorization("Bearer");

app.MapGet("/api/auth/apps/{appId}", GetApp)
    .WithName("Get App")
    .RequireAuthorization("Bearer");

app.MapPut("/api/auth/apps/{appId}", UpdateApp)
    .WithName("Update App")
    .RequireAuthorization("Bearer");

app.MapDelete("/api/auth/apps/{appId}", DeleteApp)
    .WithName("Delete App")
    .RequireAuthorization("Bearer");

// Endpoint handlers
async Task<IResult> GenerateToken(TokenGenerationRequest request, TokenService tokenService)
{
    var result = await tokenService.GenerateTokenAsync(request);
    if (!result.IsSuccess)
        return Results.BadRequest(new { error = result.ErrorMessage });
    
    return Results.Ok(new { token = result.Value, tokenType = "Bearer" });
}

async Task<IResult> CreateApp(CreateAppRequest request, AppRegistrationService appService)
{
    var app = new AppRegistration
    {
        AppId = request.AppId,
        AppName = request.AppName,
        AppSecretHash = request.AppSecretHash,
        AllowedScopes = request.AllowedScopes,
        TokenExpirationHours = request.TokenExpirationHours,
        IsActive = true
    };

    var result = await appService.CreateAsync(app);
    if (!result.IsSuccess)
        return Results.BadRequest(new { error = result.ErrorMessage });
    
    return Results.Created($"/api/auth/apps/{result.Value.AppId}", 
        new { appId = result.Value.AppId, appName = result.Value.AppName });
}

async Task<IResult> GetApp(string appId, AppRegistrationService appService)
{
    var result = await appService.GetByIdAsync(appId);
    if (!result.IsSuccess)
        return Results.NotFound(new { error = result.ErrorMessage });
    
    return Results.Ok(new { 
        appId = result.Value.AppId, 
        appName = result.Value.AppName,
        isActive = result.Value.IsActive
    });
}

async Task<IResult> UpdateApp(string appId, UpdateAppRequest request, AppRegistrationService appService)
{
    var app = new AppRegistration
    {
        AppId = appId,
        AppName = request.AppName,
        AppSecretHash = request.AppSecretHash,
        AllowedScopes = request.AllowedScopes,
        TokenExpirationHours = request.TokenExpirationHours,
        IsActive = request.IsActive
    };

    var result = await appService.UpdateAsync(appId, app);
    if (!result.IsSuccess)
        return Results.BadRequest(new { error = result.ErrorMessage });
    
    return Results.Ok(new { message = "App updated" });
}

async Task<IResult> DeleteApp(string appId, AppRegistrationService appService)
{
    var result = await appService.DeleteAsync(appId);
    if (!result.IsSuccess)
        return Results.NotFound(new { error = result.ErrorMessage });
    
    return Results.Ok(new { message = "App deleted" });
}

// DTOs
record CreateAppRequest(string AppId, string AppName, string AppSecretHash, string AllowedScopes, int? TokenExpirationHours = null);
record UpdateAppRequest(string AppName, string AppSecretHash, string AllowedScopes, int? TokenExpirationHours = null, bool IsActive = true);
