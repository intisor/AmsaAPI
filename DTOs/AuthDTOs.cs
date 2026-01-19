namespace AmsaAPI.DTOs;

/// <summary>
/// REQUESTED SCOPES: What client asks for. GRANTED SCOPES: Intersection of requested and app's allowed scopes.
/// </summary>
public class TokenGenerationRequest
{
    public string? AppId { get; set; }
    public string? AppName { get; set; }
    public required string AppSecret { get; set; }
    public int MkanId { get; set; }
    public required string[] RequestedScopes { get; set; }
}

public class TokenResponse
{
    public required string Token { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public required string TokenType { get; set; }
    public MemberTokenDto Member { get; set; } = new();
    public string[]? Roles { get; set; }
    public string[]? Scopes { get; set; }
}

public class MemberTokenDto
{
    public int MemberId { get; set;}
    public int MkanId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int UnitId { get; set; }
    public int StateId { get; set; }
    public int NationalId { get; set; }
}

public class MemberRoleContext
{
    public string[] Departments { get; set; } = [];
    public string[] Levels { get; set; } = [];
    public string[] Roles { get; set; } = [];
    public int UnitId { get; set; }
    public int StateId { get; set; }
    public int NationalId { get; set; }
    public int MkanId { get; set; }
    public int MemberId { get; set; }
    public bool IsMember { get; set; }
}
