# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade AmsaAPI.csproj
4. Run unit tests to validate upgrade in the projects listed below (if available)

## Settings

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                   | Current Version | New Version | Description                                   |
|:-----------------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.22          | 10.0.1      | Recommended for .NET 10.0                     |
| Microsoft.AspNetCore.Mvc.Testing              | 8.0.8           | 10.0.1      | Recommended for .NET 10.0                     |
| Microsoft.EntityFrameworkCore.Design          | 8.0.8           | 10.0.1      | Recommended for .NET 10.0                     |
| Microsoft.EntityFrameworkCore.InMemory        | 8.0.8           | 10.0.1      | Recommended for .NET 10.0                     |
| Microsoft.EntityFrameworkCore.SqlServer       | 8.0.8           | 10.0.1      | Recommended for .NET 10.0                     |

### Project upgrade details

#### AmsaAPI.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Authentication.JwtBearer should be updated from `8.0.22` to `10.0.1` (recommended for .NET 10.0)
  - Microsoft.AspNetCore.Mvc.Testing should be updated from `8.0.8` to `10.0.1` (recommended for .NET 10.0)
  - Microsoft.EntityFrameworkCore.Design should be updated from `8.0.8` to `10.0.1` (recommended for .NET 10.0)
  - Microsoft.EntityFrameworkCore.InMemory should be updated from `8.0.8` to `10.0.1` (recommended for .NET 10.0)
  - Microsoft.EntityFrameworkCore.SqlServer should be updated from `8.0.8` to `10.0.1` (recommended for .NET 10.0)
