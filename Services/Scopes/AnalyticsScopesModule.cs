namespace AmsaAPI.Services.Scopes;

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
