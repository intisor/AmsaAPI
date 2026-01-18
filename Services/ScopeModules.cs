namespace AmsaAPI.Services;

/// <summary>
/// Scope modules define organizational scopes for OAuth-like authorization.
/// Each module groups related scopes and specifies which require role claims.
/// </summary>

public class MemberScopesModule
{
    public const string ReadMembers = "read:members";
    public const string ExportMembers = "export:members";
    public const string VerifyMembership = "verify:membership";

    public static string[] Scopes => [ReadMembers, ExportMembers, VerifyMembership];
    public static string[] ScopesRequiringRoles => [];

    public static string GetScopeDescription(string scope) => scope switch
    {
        ReadMembers => "Read member profiles and data",
        ExportMembers => "Export member data as CSV",
        VerifyMembership => "Verify membership status",
        _ => "Unknown scope"
    };
}

public class OrganizationScopesModule
{
    public const string ReadOrganization = "read:organization";

    public static string[] Scopes => [ReadOrganization];
    public static string[] ScopesRequiringRoles => [];

    public static string GetScopeDescription(string scope) => scope switch
    {
        ReadOrganization => "Read organizational structure",
        _ => "Unknown scope"
    };
}

public class AnalyticsScopesModule
{
    public const string ReadStatistics = "read:statistics";
    public const string ReadExco = "read:exco";

    public static string[] Scopes => [ReadStatistics, ReadExco];
    public static string[] ScopesRequiringRoles => [ReadStatistics, ReadExco];

    public static string GetScopeDescription(string scope) => scope switch
    {
        ReadStatistics => "Read statistics (filtered by roles)",
        ReadExco => "Read executive committee members",
        _ => "Unknown scope"
    };
}
