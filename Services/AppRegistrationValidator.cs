using System.Text.Json;
using AmsaAPI.Common;
using AmsaAPI.Data;

namespace AmsaAPI.Services;

public static class AppRegistrationValidator
{
    public static Result<AppRegistration> ValidateForStorage(AppRegistration app)
    {
        if (app == null)
            return Result.Validation<AppRegistration>("App cannot be null");
        if (string.IsNullOrWhiteSpace(app.AppId))
            return Result.Validation<AppRegistration>("AppId required");
        if (string.IsNullOrWhiteSpace(app.AppName))
            return Result.Validation<AppRegistration>("AppName required");
        if (string.IsNullOrWhiteSpace(app.AppSecretHash))
            return Result.Validation<AppRegistration>("AppSecretHash required");
        if (string.IsNullOrWhiteSpace(app.AllowedScopes))
            return Result.Validation<AppRegistration>("AllowedScopes required");

        var scopesValidation = ValidateScopesJson(app.AllowedScopes);
        if (!scopesValidation.IsSuccess)
            return scopesValidation;
        
        if (app.TokenExpirationHours.HasValue && app.TokenExpirationHours.Value <= 0)
            return Result.Validation<AppRegistration>("TokenExpirationHours must be > 0");

        return Result.Success(app);
    }

    private static Result<AppRegistration> ValidateScopesJson(string allowedScopesJson)
    {
        string[] scopes;
        try
        {
            scopes = JsonSerializer.Deserialize<string[]>(allowedScopesJson) ?? [];
        }
        catch (JsonException ex)
        {
            return Result.Validation<AppRegistration>($"Invalid JSON: {ex.Message}");
        }

        if (scopes.Length == 0)
            return Result.Validation<AppRegistration>("At least one scope required");

        var invalid = ScopeDefinitions.GetInvalidScopes(scopes);
        if (invalid.Length > 0)
            return Result.Validation<AppRegistration>(
                $"Invalid scopes: {string.Join(", ", invalid)}");

        return Result.Success<AppRegistration>(null!);
    }
}
