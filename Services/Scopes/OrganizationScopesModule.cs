namespace AmsaAPI.Services.Scopes;

public class OrganizationScopesModule
{
    public const string ReadOrganization = "read:organization";

    public static string[] Scopes => new[] { ReadOrganization };
    public static string[] ScopesRequiringRoles => Array.Empty<string>();

    public static string GetScopeDescription(string scope) => scope switch
    {
        ReadOrganization => "Read organizational structure",
        _ => "Unknown scope"
    };
}
