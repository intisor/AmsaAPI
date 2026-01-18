using AmsaAPI.Common;
using AmsaAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace AmsaAPI.Services;

public class AppRegistrationService(AmsaDbContext db)
{
    private readonly AmsaDbContext _db = db;

    public async Task<Result<AppRegistration>> CreateAsync(AppRegistration app)
    {
        if (app == null)
            return Result.Validation<AppRegistration>("App cannot be null");
        
        var validation = AppRegistrationValidator.ValidateForStorage(app);
        if (!validation.IsSuccess)
            return validation;

        var exists = await _db.AppRegistrations.AnyAsync(a => a.AppId == app.AppId);
        if (exists)
            return Result.Conflict<AppRegistration>($"App '{app.AppId}' already exists");

        _db.AppRegistrations.Add(app);
        await _db.SaveChangesAsync();
        return Result.Success(app);
    }

    public async Task<Result<AppRegistration>> UpdateAsync(string appId, AppRegistration app)
    {
        if (app == null)
            return Result.Validation<AppRegistration>("App cannot be null");
        if (string.IsNullOrWhiteSpace(appId))
            return Result.BadRequest<AppRegistration>("AppId required");
        
        var validation = AppRegistrationValidator.ValidateForStorage(app);
        if (!validation.IsSuccess)
            return validation;

        var existing = await _db.AppRegistrations.FirstOrDefaultAsync(a => a.AppId == appId);
        if (existing == null)
            return Result.NotFound<AppRegistration>($"App '{appId}' not found");

        existing.AppName = app.AppName;
        existing.AppSecretHash = app.AppSecretHash;
        existing.AllowedScopes = app.AllowedScopes;
        existing.TokenExpirationHours = app.TokenExpirationHours;
        existing.IsActive = app.IsActive;

        _db.AppRegistrations.Update(existing);
        await _db.SaveChangesAsync();
        return Result.Success(existing);
    }

    public async Task<Result<AppRegistration>> GetByIdAsync(string appId)
    {
        if (string.IsNullOrWhiteSpace(appId))
            return Result.BadRequest<AppRegistration>("AppId required");
        
        var app = await _db.AppRegistrations.FirstOrDefaultAsync(a => a.AppId == appId);
        return app == null 
            ? Result.NotFound<AppRegistration>($"App '{appId}' not found")
            : Result.Success(app);
    }

    public Task<List<AppRegistration>> GetAllAsync() =>
        _db.AppRegistrations.ToListAsync();

    public async Task<Result<bool>> DeleteAsync(string appId)
    {
        if (string.IsNullOrWhiteSpace(appId))
            return Result.BadRequest<bool>("AppId required");
        
        var app = await _db.AppRegistrations.FirstOrDefaultAsync(a => a.AppId == appId);
        if (app == null)
            return Result.NotFound<bool>($"App '{appId}' not found");

        _db.AppRegistrations.Remove(app);
        await _db.SaveChangesAsync();
        return Result.Success(true);
    }

    public async Task<Result<bool>> UpdateLastUsedAsync(string appId)
    {
        if (string.IsNullOrWhiteSpace(appId))
            return Result.BadRequest<bool>("AppId required");
        
        var app = await _db.AppRegistrations.FirstOrDefaultAsync(a => a.AppId == appId);
        if (app == null)
            return Result.NotFound<bool>($"App '{appId}' not found");

        app.LastUsedAt = DateTime.UtcNow;
        _db.AppRegistrations.Update(app);
        await _db.SaveChangesAsync();
        return Result.Success(true);
    }
}
