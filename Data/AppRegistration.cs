using System;

namespace AmsaAPI.Data;

public partial class AppRegistration
{
    public string AppId { get; set; } = null!;

    public string AppName { get; set; } = null!;

    public string AppSecretHash { get; set; } = null!;

    public string AllowedScopes { get; set; } = null!; // JSON array: ["submit:reports", "verify:member", ...]

    public int? TokenExpirationHours { get; set; } // Nullable: use default if not set

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }
}
