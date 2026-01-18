namespace AmsaAPI.Services;

using AmsaAPI.Services.Scopes;

public static class ScopeDefinitions
{
    private static readonly Type[] _modules = [typeof(MemberScopesModule), typeof(OrganizationScopesModule), typeof(AnalyticsScopesModule)];

    public const string ReadMembers = MemberScopesModule.ReadMembers;
    public const string ExportMembers = MemberScopesModule.ExportMembers;
    public const string VerifyMembership = MemberScopesModule.VerifyMembership;
    public const string ReadOrganization = OrganizationScopesModule.ReadOrganization;
    public const string ReadStatistics = AnalyticsScopesModule.ReadStatistics;
    public const string ReadExco = AnalyticsScopesModule.ReadExco;

    public static readonly string[] AllValidScopes =
    [
        .. MemberScopesModule.Scopes,
        .. OrganizationScopesModule.Scopes,
        .. AnalyticsScopesModule.Scopes
    ];

    public static readonly string[] ScopesRequiringRoles =
    [
        .. MemberScopesModule.ScopesRequiringRoles,
        .. OrganizationScopesModule.ScopesRequiringRoles,
        .. AnalyticsScopesModule.ScopesRequiringRoles
    ];

    public static bool RequiresRoles(string[] grantedScopes) =>
        grantedScopes.Any(scope => ScopesRequiringRoles.Contains(scope));

    public static bool AreValidScopes(string[] requestedScopes) =>
        requestedScopes.All(scope => AllValidScopes.Contains(scope));

    public static string[] GetInvalidScopes(string[] requestedScopes) =>
        [.. requestedScopes.Except(AllValidScopes)];

    public static string GetScopeDescription(string scope)
    {
        if (MemberScopesModule.Scopes.Contains(scope))
            return MemberScopesModule.GetScopeDescription(scope);
        if (OrganizationScopesModule.Scopes.Contains(scope))
            return OrganizationScopesModule.GetScopeDescription(scope);
        if (AnalyticsScopesModule.Scopes.Contains(scope))
            return AnalyticsScopesModule.GetScopeDescription(scope);
        return "Unknown scope";
    }
}