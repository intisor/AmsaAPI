# AmsaAPI Unit Tests

This document provides an overview of the unit test structure for the AmsaAPI project, covering both Minimal API and FastEndpoint implementations.

## Project Structure

```
AmsaAPI.Tests/
├── Infrastructure/          # Test infrastructure and base classes
│   ├── AmsaApiWebApplicationFactory.cs    # WebApplicationFactory for integration tests
│   ├── IntegrationTestBase.cs             # Base class for all integration tests
│   ├── TokenTestHelper.cs                 # JWT token generation for testing
│   └── DbSeedHelper.cs                    # Database seeding utilities
├── Endpoints/               # Minimal API endpoint tests
│   ├── MemberEndpointsTests.cs
│   └── DepartmentEndpointsTests.cs
├── FastEndpoints/           # FastEndpoint framework tests
│   ├── OrganizationFastEndpointsTests.cs
│   └── MemberFastEndpointsTests.cs
├── Authentication/          # Authentication and authorization tests
│   └── AuthenticationTests.cs
├── Utilities/               # Test utilities and helpers
│   ├── TestDataBuilder.cs               # Fluent builder for test entities
│   └── CustomAssertions.cs              # Custom assertion helpers
├── appsettings.json         # Test configuration
└── README.md                # This file
```

## Test Infrastructure

### AmsaApiWebApplicationFactory
Custom `WebApplicationFactory` that configures the application for testing:
- Uses in-memory SQLite database for test isolation
- Each test gets a fresh database instance
- Removes production dependencies and replaces them with test-friendly alternatives

### IntegrationTestBase
Abstract base class providing common functionality:
- Automatic setup/teardown via `IAsyncLifetime`
- `HttpClient` for making test requests
- `DbContext` for database operations
- Helper methods: `RefreshEntityAsync()`, `EntityExistsAsync()`

### TokenTestHelper
Static helper for JWT token generation:
- `GenerateTestToken()` - Creates valid JWT tokens with configurable claims
- `GenerateExpiredToken()` - Creates tokens that have already expired
- Used for authentication testing

### DbSeedHelper
Static helper for seeding test data:
- `SeedMembersAsync()` - Creates member records with required relationships
- `SeedUnitsAsync()` - Creates unit records with state/national relationships
- `SeedDepartmentsAsync()` - Creates department records

## Test Categories

### 1. Minimal API Endpoint Tests

#### MemberEndpointsTests
Tests the `/api/minimal/members/*` endpoints:
- ✅ `GetAllMembers_WithMembers_ReturnsOkWithMembers` - GET / returns all members
- ✅ `GetAllMembers_WithoutMembers_ReturnsOkWithEmptyList` - GET / with no data returns empty list
- ✅ `GetMemberById_WithValidId_ReturnsOkWithMember` - GET /{id} returns specific member
- ✅ `GetMemberById_WithInvalidId_ReturnsNotFound` - GET /{id} with invalid ID returns 404
- ✅ `GetMemberByMkanId_WithValidMkanId_ReturnsOkWithMember` - GET /mkan/{id} returns member
- ✅ `GetMemberByMkanId_WithInvalidMkanId_ReturnsNotFound` - GET /mkan/{id} with invalid ID returns 404
- ✅ `GetMembersByUnit_WithValidUnitId_ReturnsOkWithMembers` - GET /unit/{id} returns unit members
- ✅ `GetMembersByDepartment_WithValidDepartmentId_ReturnsOkWithMembers` - GET /department/{id}
- ✅ `SearchMembersByName_WithValidName_ReturnsOkWithMembers` - GET /search/{name}
- ✅ `CreateMember_WithValidData_ReturnsCreatedAtRoute` - POST / creates new member
- ✅ `UpdateMember_WithValidData_ReturnsNoContent` - PUT /{id} updates member
- ✅ `DeleteMember_WithValidId_ReturnsNoContent` - DELETE /{id} removes member

#### DepartmentEndpointsTests
Tests the `/api/minimal/departments/*` endpoints:
- ✅ `GetAllDepartments_WithDepartments_ReturnsOkWithDepartments` - GET /
- ✅ `GetAllDepartments_WithoutDepartments_ReturnsOkWithEmptyList` - GET / with no data
- ✅ `GetDepartmentById_WithValidId_ReturnsOkWithDepartment` - GET /{id}
- ✅ `GetDepartmentById_WithInvalidId_ReturnsNotFound` - GET /{id} with invalid ID

### 2. FastEndpoint Tests

#### OrganizationFastEndpointsTests
Tests FastEndpoint implementations for organizations:
- `GetAllUnitsEndpointTests`
  - ✅ `GetAllUnits_WithUnits_ReturnsOkWithUnitsList` - Returns all units
  - ✅ `GetAllUnits_WithoutUnits_ReturnsOkWithEmptyList` - Returns empty list when no units
  - ✅ `GetAllUnits_UnitsAreSortedByStateAndName` - Verifies sorting order

- `GetUnitByIdEndpointTests`
  - ✅ `GetUnitById_WithValidId_ReturnsOkWithUnitDetails` - Returns specific unit
  - ✅ `GetUnitById_WithInvalidId_ReturnsNotFound` - Returns 404 for invalid ID
  - ✅ `GetUnitById_IncludesUnitMembers` - Verifies member data is included

#### MemberFastEndpointsTests
Tests FastEndpoint implementations for members:
- `GetAllMembersEndpointTests`
  - ✅ `GetAllMembers_WithMembers_ReturnsOkWithMembersList` - Returns all members
  - ✅ `GetAllMembers_WithoutMembers_ReturnsOkWithEmptyList` - Returns empty list

- `GetMemberByIdFastEndpointTests`
  - ✅ `GetMemberById_WithValidId_ReturnsOkWithMemberDetails` - Returns specific member
  - ✅ `GetMemberById_WithInvalidId_ReturnsNotFound` - Returns 404

- `CreateMemberFastEndpointTests`
  - ✅ `CreateMember_WithValidData_ReturnsCreatedStatus` - Creates member successfully
  - ✅ `CreateMember_WithDuplicateMkanId_ReturnsBadRequest` - Rejects duplicate MKAN ID

### 3. Authentication Tests

#### AuthenticationTests
Tests JWT authentication and authorization:
- ✅ `ValidToken_WithAuthenticatedEndpoint_AllowsAccess` - Valid token grants access
- ✅ `InvalidToken_WithAuthenticatedEndpoint_ReturnsForbidden` - Invalid token denied
- ✅ `ExpiredToken_WithAuthenticatedEndpoint_ReturnsForbidden` - Expired token denied
- ✅ `NoToken_WithAuthenticatedEndpoint_ReturnsForbidden` - Missing token denied
- ✅ `ValidToken_WithWrongAudience_ReturnsForbidden` - Wrong audience rejected

#### EndpointAuthorizationTests
Tests endpoint-level authorization:
- ✅ `AllowAnonymousEndpoint_WithoutToken_ReturnsOk` - Anonymous endpoints accessible
- ✅ `ProtectedEndpoint_WithValidToken_AllowsAccess` - Protected endpoints require valid token

## Test Utilities

### TestDataBuilder
Fluent API for building test entities:

```csharp
var member = TestDataBuilder.CreateMember()
    .WithFirstName("John")
    .WithLastName("Doe")
    .WithUnitId(1)
    .Build();

var unit = TestDataBuilder.CreateUnit()
    .WithUnitName("Test Unit")
    .WithStateId(1)
    .Build();
```

### CustomAssertions
Common assertion patterns:

```csharp
// Assert token validity
CustomAssertions.AssertTokenIsValid(token);

// Assert recent timestamp
CustomAssertions.AssertRecentTimestamp(DateTime.UtcNow);

// Assert equal responses
await CustomAssertions.AssertResponsesEqualAsync<UnitDto>(expected, actual);
```

## Running Tests

### Via Visual Studio Test Explorer
1. Open Test Explorer (View > Test Explorer or Ctrl+E, T)
2. Right-click on test class/method and select "Run"
3. View results in the Test Explorer window

### Via dotnet CLI
```bash
# Run all tests
dotnet test

# Run tests in specific project
dotnet test AmsaAPI.Tests

# Run specific test class
dotnet test AmsaAPI.Tests --filter "ClassName=MemberEndpointsTests"

# Run with verbose output
dotnet test -v detailed

# Run with code coverage
dotnet-coverage collect -f cobertura -o coverage.cobertura.xml dotnet test
```

## Test Configuration

The test project uses:
- **Framework**: xUnit 2.9.2
- **Infrastructure**: Microsoft.AspNetCore.Mvc.Testing (WebApplicationFactory)
- **Database**: SQLite in-memory for fast, isolated tests
- **JWT**: System.IdentityModel.Tokens.Jwt for token generation

## Database Testing Strategy

Each test:
1. Gets a fresh in-memory database instance
2. Seeds required data using `DbSeedHelper`
3. Executes test assertions
4. Database is automatically cleaned up after test completes

This ensures:
- **Isolation**: Tests don't interfere with each other
- **Speed**: In-memory database is fast
- **Consistency**: Each test starts with known state

## Adding New Tests

### 1. For a Minimal API Endpoint
```csharp
public class YourEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task YourMethod_WithCondition_ExpectedResult()
    {
        // Arrange
        await DbSeedHelper.SeedMembersAsync(DbContext);

        // Act
        var response = await Client.GetAsync("/api/minimal/endpoint");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

### 2. For a FastEndpoint
```csharp
public class YourFastEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task YourHandler_WithCondition_ExpectedResult()
    {
        // Arrange
        var request = new YourRequest { /* ... */ };

        // Act
        var response = await Client.PostAsJsonAsync("/api/endpoint", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

### 3. For Authentication
```csharp
[Fact]
public async Task ProtectedEndpoint_WithValidToken_AllowsAccess()
{
    // Arrange
    var token = TokenTestHelper.GenerateTestToken();
    Client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.GetAsync("/api/protected");

    // Assert
    Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

## Best Practices

1. **One assertion per concept** - Multiple assertions in one test are OK if testing related outcomes
2. **Descriptive names** - Test names should clearly describe what they test
3. **Arrange-Act-Assert** - Follow AAA pattern for clarity
4. **Use builders** - Use `TestDataBuilder` for creating complex test data
5. **Fresh data per test** - Never rely on data from other tests
6. **Test behaviors, not implementation** - Test public API contracts
7. **Mock external dependencies** - Use mocks for third-party services

## Common Issues

### Tests not discovered
- Ensure test classes inherit from `IntegrationTestBase`
- Ensure test methods are public and decorated with `[Fact]`
- Rebuild the solution

### Database connection errors
- Verify `appsettings.json` exists in test project
- Ensure SQLite NuGet package is installed
- Check that `AmsaDbContext` DbSeedHelper properly initializes foreign keys

### JWT token validation errors
- Verify token generated with correct secret key
- Check issuer and audience match configuration
- Use `TokenTestHelper` for consistent token generation

## Coverage Goals

- **Unit Tests**: 80%+ coverage for business logic
- **Integration Tests**: Key user workflows and API contracts
- **Authentication**: All auth paths (valid, invalid, expired, wrong audience)
- **Error Handling**: 4xx and 5xx response scenarios

Run coverage reports with:
```bash
dotnet-coverage collect -f cobertura -o coverage.cobertura.xml dotnet test
```
