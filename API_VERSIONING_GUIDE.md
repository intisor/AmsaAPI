# AMSA Nigeria API - Clean & Organized

A modern, high-performance API for AMSA Nigeria organizational data featuring both **FastEndpoints** (modern) and **Minimal API** (traditional) approaches.

## Quick Start

```bash
dotnet run
```

Visit: `http://localhost:5000/test.html`

## Project Structure

### ?? FastEndpoints (Organized Groups)
```
FastEndpoints/
??? MemberFastEndpoints.cs        # All member-related endpoints
??? OrganizationFastEndpoints.cs  # Units, States, Nationals endpoints  
??? DepartmentFastEndpoints.cs    # Department endpoints
??? StatisticsFastEndpoints.cs    # Dashboard & analytics endpoints
```

### ?? Minimal API (Organized Groups)
```
Endpoints/
??? MemberEndpoints.cs        # Member CRUD operations
??? OrganizationEndpoints.cs  # Organization CRUD operations
??? DepartmentEndpoints.cs    # Department CRUD operations
??? StatisticsEndpoints.cs    # Statistics & analytics
??? ImportEndpoints.cs        # CSV import functionality
```

## API Architecture Comparison

### ?? FastEndpoints (Modern, Read-Optimized)
**Base URL:** `/api/*`
- 4 organized endpoint files
- Enhanced performance with optimized queries
- Rich response DTOs with relationships
- Modern async patterns

### ?? Minimal API (Traditional, Full CRUD)
**Base URL:** `/api/minimal/*`
- 5 organized endpoint files  
- Complete CRUD operations
- Traditional REST patterns
- Full data management capabilities

## FastEndpoints

### ???????? Members (`MemberFastEndpoints.cs`)
```
GET /api/members                    - All members with hierarchy
GET /api/members/{id}               - Member details with roles
GET /api/members/mkan/{id}          - Member by MKAN ID
GET /api/members/unit/{unitId}      - Unit members
GET /api/members/department/{id}    - Department members
GET /api/members/search/{name}      - Search by name
```

### ?? Organization (`OrganizationFastEndpoints.cs`)
```
GET /api/units                      - All units with stats
GET /api/units/{id}                 - Unit details with members
GET /api/units/state/{stateId}      - Units by state
GET /api/states                     - All states with stats  
GET /api/states/{id}                - State details
GET /api/nationals                  - All nationals with stats
GET /api/nationals/{id}             - National details
```

### ??? Departments (`DepartmentFastEndpoints.cs`)
```
GET /api/departments                - All departments
GET /api/departments/{id}           - Department details
```

### ?? Statistics (`StatisticsFastEndpoints.cs`)
```
GET /api/stats/dashboard            - Dashboard statistics
GET /api/stats/organization-summary - Organization overview
GET /api/hierarchy                  - Complete hierarchy
```

## Minimal API (Full CRUD)

### ???????? Members (`MemberEndpoints.cs`)
```
GET    /api/minimal/members              - All members
GET    /api/minimal/members/{id}         - Member by ID
GET    /api/minimal/members/mkan/{id}    - Member by MKAN ID
GET    /api/minimal/members/unit/{id}    - Members by unit
GET    /api/minimal/members/department/{id} - Members by department
GET    /api/minimal/members/search/{name} - Search members
POST   /api/minimal/members              - Create member
PUT    /api/minimal/members/{id}         - Update member
DELETE /api/minimal/members/{id}         - Delete member
```

### ?? Organization (`OrganizationEndpoints.cs`)
```
Units, States, Nationals with full CRUD operations
GET /api/minimal/hierarchy - Organization hierarchy
```

### ??? Departments (`DepartmentEndpoints.cs`)
```
Full CRUD operations for department management
```

### ?? Statistics (`StatisticsEndpoints.cs`)
```
Comprehensive analytics and reporting endpoints
```

### ?? Import (`ImportEndpoints.cs`)
```
GET  /api/import/test                   - Test CSV availability
GET  /api/import/exco                   - Import default EXCO data
POST /api/import/exco/upload            - Upload & import CSV
```

## File Organization Benefits

### ? **Before (Scattered)**
- 18+ individual endpoint files
- Hard to navigate and maintain
- Duplicated imports and patterns
- Difficult to find related functionality

### ? **After (Organized)**
- **FastEndpoints**: 4 organized files
- **Minimal API**: 5 organized files
- Related endpoints grouped together
- Easy to navigate and maintain
- Consistent patterns within each group

## When to Use What

### Use **FastEndpoints** when:
- ? You need **optimized read performance**
- ? You want **rich, hierarchical responses**
- ? You're building **dashboards or analytics**
- ? You need **modern API patterns**

### Use **Minimal API** when:
- ? You need **full CRUD operations**
- ? You're doing **data management**
- ? You want **traditional REST patterns**
- ? You need **create/update/delete functionality**

## Technology Stack

- **.NET 8** - Latest framework
- **FastEndpoints** - Modern API framework
- **Minimal API** - Traditional ASP.NET Core
- **Entity Framework Core** - Advanced ORM
- **SQL Server** - Database platform

## Key Features

- ?? **Dual Architecture** - Choose the right tool for the job
- ?? **Organized Structure** - Grouped endpoints by functionality
- ??? **Comprehensive** - Full CRUD + optimized reads
- ?? **Well Documented** - Clear endpoint organization
- ?? **Search Capable** - Full-text search on members
- ?? **Analytics Ready** - Rich statistical endpoints
- ??? **Maintainable** - Clean, organized codebase

This organized structure makes the codebase **much easier to navigate and maintain** while providing the best of both worlds - modern FastEndpoints and comprehensive Minimal API functionality! ??