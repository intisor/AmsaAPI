# ??? AMSA Nigeria API - Production Architecture

## ?? Project Structure

```
AmsaAPI/
??? Endpoints/                    # API Endpoints (Separated by Feature)
?   ??? MemberEndpoints.cs       # Member CRUD operations
?   ??? StatisticsEndpoints.cs   # Dashboard and analytics
?   ??? ImportEndpoints.cs       # CSV import functionality
?   ??? DepartmentEndpoints.cs   # Department management
?   ??? OrganizationEndpoints.cs # Units, States, Nationals
??? Data/                        # Entity Framework models
??? DTOs/                        # Data Transfer Objects
??? Extensions/                  # Extension methods for mapping
??? Migrations/                  # Database migrations
??? Program.cs                   # Application startup
??? ExcoImporter.cs             # CSV import logic
```

## ?? Design Principles Applied

### ? **1. Separation of Concerns**
- Each endpoint file handles one specific domain
- Clear responsibility boundaries
- Easy to locate and modify specific functionality

### ? **2. Reduced Abstraction**
- Direct, explicit code instead of complex abstractions
- Clear SQL queries using `FromSqlRaw` with string literals
- Straightforward error handling with try-catch blocks

### ? **3. Production-Ready Features**
- **Proper error handling** - All endpoints wrapped in try-catch
- **Input validation** - Explicit checks for required data
- **Resource cleanup** - Proper disposal of temporary files
- **Security** - Parameterized queries to prevent SQL injection
- **Performance** - Raw SQL queries for optimal database performance

### ? **4. Maintainability**
- **Clear method names** - Self-documenting code
- **Consistent patterns** - Same structure across all endpoints
- **Minimal dependencies** - Each endpoint file is self-contained
- **Easy testing** - Each method can be tested independently

## ?? Endpoint Organization

### ???????? **MemberEndpoints.cs**
```csharp
GET    /api/members                    # Get all members with roles
GET    /api/members/{id}               # Get member by ID
GET    /api/members/mkan/{mkanId}      # Get member by MKAN ID
GET    /api/members/unit/{unitId}      # Get members by unit
GET    /api/members/department/{id}    # Get members by department
GET    /api/members/search/{name}      # Search members by name
POST   /api/members                    # Create new member
PUT    /api/members/{id}               # Update member
DELETE /api/members/{id}               # Delete member
```

### ?? **StatisticsEndpoints.cs**
```csharp
GET /api/stats/dashboard               # Dashboard statistics
GET /api/stats/units                   # Unit statistics
GET /api/stats/departments             # Department statistics
GET /api/stats/organization-summary    # Complete organization overview
```

### ?? **ImportEndpoints.cs**
```csharp
GET  /api/import/test                  # Test if CSV file exists
GET  /api/import/exco                  # Import from default CSV
POST /api/import/exco/upload           # Upload and import CSV
```

### ?? **DepartmentEndpoints.cs**
```csharp
GET    /api/departments                # Get all departments
GET    /api/departments/{id}           # Get department details
POST   /api/departments                # Create department
PUT    /api/departments/{id}           # Update department
DELETE /api/departments/{id}           # Delete department
```

### ??? **OrganizationEndpoints.cs**
```csharp
# Units
GET    /api/units                      # Get all units
GET    /api/units/{id}                 # Get unit details
GET    /api/units/state/{stateId}      # Get units by state
POST   /api/units                      # Create unit
PUT    /api/units/{id}                 # Update unit
DELETE /api/units/{id}                 # Delete unit

# States
GET  /api/states                       # Get all states
GET  /api/states/{id}                  # Get state details
POST /api/states                       # Create state

# Nationals
GET /api/nationals                     # Get all nationals
GET /api/nationals/{id}                # Get national details

# Hierarchy
GET /api/hierarchy                     # Get complete org hierarchy
```

## ?? **Key Improvements Made**

### **1. Modular Architecture**
- **Single Responsibility**: Each file handles one domain
- **Easy Navigation**: Find features quickly
- **Independent Testing**: Test each module separately
- **Team Development**: Multiple developers can work simultaneously

### **2. Performance Optimizations**
```csharp
// Raw SQL with proper joins
var memberData = await db.Database.SqlQueryRaw<MemberWithHierarchyDto>("""
    SELECT m.MemberId, m.FirstName, m.LastName, m.Email, m.Phone, m.Mkanid,
           u.UnitId, u.UnitName, u.StateId,
           s.StateName, s.NationalId, n.NationalName
    FROM Members m
    INNER JOIN Units u ON m.UnitId = u.UnitId
    INNER JOIN States s ON u.StateId = s.StateId
    INNER JOIN Nationals n ON s.NationalId = n.NationalId
    """).ToListAsync();
```

### **3. Clear Error Handling**
```csharp
try
{
    // Business logic here
    return Results.Ok(response);
}
catch (Exception ex)
{
    return Results.Problem($"Error message: {ex.Message}");
}
```

### **4. Explicit Validation**
```csharp
// Check if MKAN ID already exists
var existingMember = await db.Members
    .AsNoTracking()
    .FirstOrDefaultAsync(m => m.Mkanid == request.Mkanid);

if (existingMember != null)
    return Results.BadRequest($"Member with MKAN ID {request.Mkanid} already exists");
```

### **5. Resource Management**
```csharp
try
{
    // File operations
}
finally
{
    // Clean up temporary file
    if (File.Exists(tempPath))
    {
        File.Delete(tempPath);
    }
}
```

## ?? **Testing Strategy**

### **Unit Testing**
Each endpoint method is static and testable:
```csharp
[Test]
public async Task GetMemberById_ReturnsCorrectMember()
{
    // Arrange
    var db = CreateTestDatabase();
    
    // Act
    var result = await MemberEndpoints.GetMemberById(1, db);
    
    // Assert
    Assert.IsType<Ok<MemberDetailResponse>>(result);
}
```

### **Integration Testing**
Test complete endpoint flows:
```csharp
[Test]
public async Task CreateMember_ValidData_ReturnsCreated()
{
    // Test the complete flow from HTTP request to database
}
```

## ?? **Benefits of This Architecture**

1. **?? Easy Debugging** - Clear stack traces, simple to follow
2. **? Better Performance** - Optimized SQL queries, minimal overhead
3. **??? Simple Maintenance** - Find and fix issues quickly
4. **?? Self-Documenting** - Code tells the story clearly
5. **?? Production Ready** - Proper error handling and validation
6. **?? Team Friendly** - Multiple developers can work without conflicts

## ?? **Getting Started**

1. **Clone the repository**
2. **Update connection string** in `appsettings.json`
3. **Run migrations**: `dotnet ef database update`
4. **Start the application**: `dotnet run`
5. **Test endpoints**: Visit `/test.html` for interactive testing

The API is now structured for production use with clear separation of concerns, excellent performance, and maintainable code! ??

## Architecture Decision Records
For detailed rationale behind architectural choices, see the [ADR Index](docs/adr/adr-index.md) and [ADR Process](docs/adr/ADR-PROCESS.md).