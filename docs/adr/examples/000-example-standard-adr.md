# ADR 000: Example Standard ADR - Choosing Logging Framework

**Status**: Accepted

**Date**: 2025-09-27

**Technical Story**: The application needs structured logging for debugging API endpoints and import processes.

## Context
Current setup uses basic Console.WriteLine in endpoints like MemberEndpoints.cs. For production, we need persistent, searchable logs integrated with Azure or ELK stack.

## Decision
Adopt Serilog as the logging framework. Configure in Program.cs with sinks for file and console. Use structured logging in endpoints (e.g., Log.Information("Member imported: {Mkanid}", member.Mkanid)).

## Alternatives Considered
- **Alternative 1: Built-in Microsoft.Extensions.Logging**: Pros: No dependencies, simple. Cons: Limited structured logging, no easy file rotation.
- **Alternative 2: NLog**: Pros: Mature, good performance. Cons: Heavier config, less .NET Core native.
- **Alternative 3: Continue with Console**: Pros: Zero cost. Cons: No persistence, hard to debug in production.

Serilog chosen for its ease of structured logging and extensibility.

## Consequences
- **Positive**: Better observability, easy to query logs for member import errors.
- **Negative**: Adds NuGet dependency (Serilog.AspNetCore).
- **Neutral**: Minimal performance impact for logging volume.

## Implementation Notes
- Add to AmsaAPI.csproj: `<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />`
- Configure in Program.cs: `builder.Host.UseSerilog()`
- Update endpoints to use ILogger<T>.
- Test: Run import and check log files.

## Related ADRs
- None (initial example).
