namespace AmsaAPI.Services.Scopes;

public class MemberScopesModule
{
    public const string ReadMembers = "read:members";
    public const string ExportMembers = "export:members";
    public const string VerifyMembership = "verify:membership";

    public static string[] Scopes => new[] { ReadMembers, ExportMembers, VerifyMembership };
    public static string[] ScopesRequiringRoles => Array.Empty<string>();

    public static string GetScopeDescription(string scope) => scope switch
    {
        ReadMembers => "Read member profiles and data",
        ExportMembers => "Export member data as CSV",
        VerifyMembership => "Verify membership status",
        _ => "Unknown scope"
    };
}
