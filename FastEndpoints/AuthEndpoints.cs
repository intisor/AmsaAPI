using FastEndpoints;
using AmsaAPI.Services;
using AmsaAPI.DTOs;
using AmsaAPI.Data;
using AmsaAPI.Common;

namespace AmsaAPI.FastEndpoints;

/// <summary>
/// Generate JWT token for external applications
/// </summary>
public class GenerateTokenEndpoint : Endpoint<TokenGenerationRequest, object>
{
    private readonly TokenService _tokenService;

    public GenerateTokenEndpoint(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public override void Configure()
    {
        Post("/api/auth/token");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Generate JWT token";
            s.Description = "Generate a JWT token for an external application based on requested scopes";
        });
    }

    public override async Task HandleAsync(TokenGenerationRequest request, CancellationToken ct)
    {
        var result = await _tokenService.GenerateTokenAsync(request);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorType switch
            {
                ErrorType.NotFound => 404,
                ErrorType.Validation => 400,
                _ => 400
            };

            Response = new { error = result.ErrorMessage };
            HttpContext.Response.StatusCode = statusCode;
            return;
        }

        Response = new { token = result.Value, tokenType = "Bearer" };
        await Send.OkAsync(Response,ct);
    }
}

/// <summary>
/// Create new app registration (Admin only)
/// </summary>
public class CreateAppEndpoint : Endpoint<CreateAppRequest, object>
{
    private readonly AppRegistrationService _appService;

    public CreateAppEndpoint(AppRegistrationService appService)
    {
        _appService = appService;
    }

    public override void Configure()
    {
        Post("/api/auth/apps");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Create app registration";
            s.Description = "Register a new external application with allowed scopes";
        });
    }

    public override async Task HandleAsync(CreateAppRequest request, CancellationToken ct)
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

        var result = await _appService.CreateAsync(app);

        if (!result.IsSuccess)
        {
            Response = new { error = result.ErrorMessage };
            HttpContext.Response.StatusCode = result.ErrorType == ErrorType.Conflict ? 409 : 400;
            return;
        }

        Response = new { appId = result.Value.AppId, appName = result.Value.AppName };
        HttpContext.Response.StatusCode = 201;
        await Send.OkAsync(ct);
    }
}

/// <summary>
/// Get app details (Admin only)
/// </summary>
public class GetAppEndpoint : Endpoint<GetAppRequest, object>
{
    private readonly AppRegistrationService _appService;

    public GetAppEndpoint(AppRegistrationService appService)
    {
        _appService = appService;
    }

    public override void Configure()
    {
        Get("/api/auth/apps/{appId}");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Get app details";
        });
    }

    public override async Task HandleAsync(GetAppRequest request, CancellationToken ct)
    {
        var result = await _appService.GetByIdAsync(request.AppId!);

        if (!result.IsSuccess)
        {
            Response = new { error = result.ErrorMessage };
            HttpContext.Response.StatusCode = 404;
            return;
        }

        Response = new
        {
            appId = result.Value.AppId,
            appName = result.Value.AppName,
            isActive = result.Value.IsActive,
            createdAt = result.Value.CreatedAt,
            lastUsedAt = result.Value.LastUsedAt
        };

        await Send.OkAsync(Response,ct);
    }
}

/// <summary>
/// Update app configuration (Admin only)
/// </summary>
public class UpdateAppEndpoint : Endpoint<UpdateAppRequest>
{
    private readonly AppRegistrationService _appService;

    public UpdateAppEndpoint(AppRegistrationService appService)
    {
        _appService = appService;
    }

    public override void Configure()
    {
        Put("/api/auth/apps/{appId}");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Update app configuration";
        });
    }

    public override async Task HandleAsync(UpdateAppRequest request, CancellationToken ct)
    {
        var app = new AppRegistration
        {
            AppId = request.AppId!,
            AppName = request.AppName!,
            AppSecretHash = request.AppSecretHash!,
            AllowedScopes = request.AllowedScopes!,
            TokenExpirationHours = request.TokenExpirationHours,
            IsActive = request.IsActive
        };

        var result = await _appService.UpdateAsync(request.AppId!, app);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = result.ErrorType == ErrorType.NotFound ? 404 : 400;
            return;
        }

        await Send.OkAsync(ct);
    }
}

/// <summary>
/// Delete app registration (Admin only)
/// </summary>
public class DeleteAppEndpoint : Endpoint<DeleteAppRequest>
{
    private readonly AppRegistrationService _appService;

    public DeleteAppEndpoint(AppRegistrationService appService)
    {
        _appService = appService;
    }

    public override void Configure()
    {
        Delete("/api/auth/apps/{appId}");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Delete app registration";
        });
    }

    public override async Task HandleAsync(DeleteAppRequest request, CancellationToken ct)
    {
        var result = await _appService.DeleteAsync(request.AppId!);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 404;
            return;
        }

        await Send.OkAsync(ct);
    }
}

// DTOs
public class CreateAppRequest
{
    public required string AppId { get; set; }
    public required string AppName { get; set; }
    public required string AppSecretHash { get; set; }
    public required string AllowedScopes { get; set; }
    public int? TokenExpirationHours { get; set; }
}

public class GetAppRequest
{
    public string? AppId { get; set; }
}

public class UpdateAppRequest
{
    public string? AppId { get; set; }
    public string? AppName { get; set; }
    public string? AppSecretHash { get; set; }
    public string? AllowedScopes { get; set; }
    public int? TokenExpirationHours { get; set; }
    public bool IsActive { get; set; }
}

public class DeleteAppRequest
{
    public string? AppId { get; set; }
}
