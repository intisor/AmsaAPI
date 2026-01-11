# 🕌 AMSA Nigeria API

> **Ahmadiyya Muslim Students Association - Nigeria Chapter Management System**

A comprehensive RESTful API built with .NET 9 for managing the organizational structure, members, and executive roles of the Ahmadiyya Muslim Students Association (AMSA) Nigeria. AMSA is a subsidiary of the Ahmadiyya Muslim Organization in Nigeria. This system handles the hierarchical structure from National to State to Unit levels, with sophisticated role management capabilities.

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat&logo=dotnet)
![Entity Framework](https://img.shields.io/badge/Entity%20Framework-Core%209.0-512BD4?style=flat)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-CC2927?style=flat&logo=microsoft-sql-server)

## 📋 Table of Contents

- [🚀 Getting Started](#-getting-started)
- [🏗️ Database Schema](#️-database-schema)
- [📊 API Endpoints Overview](#-api-endpoints-overview)
- [👥 Member Management](#-member-management)
- [🏢 Organization Management](#-organization-management)
- [🔧 Role Assignment](#-role-assignment)
- [📈 Statistics & Analytics](#-statistics--analytics)
- [📥 Import & Export](#-import--export)
- [🔍 Advanced Queries](#-advanced-queries)
- [💾 Bulk Operations](#-bulk-operations)
- [🧪 Testing](#-testing)
- [🚀 Deployment](#-deployment)
- [## Architecture Decision Records](#architecture-decision-records)

## 🚀 Getting Started

### Prerequisites

- **.NET 9 SDK** or later
- **SQL Server** (LocalDB, Express, or Full)
- **Visual Studio 2022** or **VS Code**

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd AmsaAPI
   ```

2. **Configure Database Connection**
   ```json
   // appsettings.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AmsaDB;Trusted_Connection=true;MultipleActiveResultSets=true"
     }
   }
   ```

3. **Run Database Migrations**
   ```bash
   dotnet ef database update
   ```

4. **Build and Run**
   ```bash
   dotnet build
   dotnet run
   ```

5. **Access the API**
   - **API Base URL**: `https://localhost:7xxx`
   - **Test Interface**: `https://localhost:7xxx/test.html`
   - **OpenAPI/Swagger**: `https://localhost:7xxx/openapi` (Development only)

## 🏗️ Database Schema

The API manages a hierarchical organizational structure:

```
National (Nigeria)
└── States (36 States + FCT)
    └── Units (AMSA Chapters in Schools/Institutions)
        └── Members (Student Members)
            └── EXCO Roles (Executive Positions)
```

### Core Entities

| Entity | Description | Key Fields |
|--------|-------------|------------|
| **National** | Country level (Nigeria) | `NationalId`, `NationalName` |
| **State** | State/Region level | `StateId`, `StateName`, `NationalId` |
| **Unit** | AMSA Chapter/Institution | `UnitId`, `UnitName`, `StateId` |
| **Member** | Individual student members | `MemberId`, `FirstName`, `LastName`, `Email`, `Phone`, `MKANID`, `UnitId` |
| **Department** | Executive departments | `DepartmentId`, `DepartmentName` |
| **Level** | Organizational levels (National/State/Unit) | `LevelId`, `LevelType`, `NationalId?`, `StateId?`, `UnitId?` |
| **LevelDepartment** | Department-Level associations | `LevelDepartmentId`, `LevelId`, `DepartmentId` |
| **MemberLevelDepartment** | EXCO role assignments | `MemberLevelDepartmentId`, `MemberId`, `LevelDepartmentId` |

## 📊 API Endpoints Overview

The API provides **50+ endpoints** organized into logical groups:

| Category | Endpoints | Description |
|----------|-----------|-------------|
| 👥 **Members** | 10 | Member CRUD, search, filtering |
| 🏢 **Organization** | 15 | Units, States, Nationals, Departments |
| 🔧 **Roles** | 8 | EXCO role assignments and management |
| 📈 **Statistics** | 6 | Dashboard data and analytics |
| 🔍 **Advanced** | 8 | Complex queries and reports |
| 📥 **Import/Export** | 4 | CSV import and data operations |
| 💾 **Bulk** | 3 | Mass operations and cleanup |

---

## 👥 Member Management

### Core Member Operations

#### Get All Members
```http
GET /api/members
```
**Response**: Complete member list with organizational hierarchy and roles
```json
{
  "memberId": 1,
  "firstName": "Ahmad",
  "lastName": "Kareem",
  "email": "ahmad.kareem@example.com",
  "phone": "+234123456789",
  "mkanid": 1001,
  "unit": {
    "unitId": 5,
    "unitName": "University of Lagos",
    "state": {
      "stateId": 25,
      "stateName": "Lagos",
      "national": {
        "nationalId": 1,
        "nationalName": "Nigeria"
      }
    }
  },
  "roles": [
    {
      "departmentName": "Dawah",
      "levelType": "President",
      "scope": "Unit"
    }
  ]
}
```

#### Get Member by ID
```http
GET /api/members/{id}
```

#### Get Member by MKAN ID
```http
GET /api/members/mkan/{mkanId}
```

#### Search Members by Name
```http
GET /api/members/search/{name}
```

### Filtered Member Queries

#### Get Members by Unit
```http
GET /api/members/unit/{unitId}
```

#### Get Members by Department
```http
GET /api/members/department/{departmentId}
```

#### Get Members by Level Type
```http
GET /api/members/level/{levelType}
```
**Example**: `/api/members/level/President` - Returns all Presidents across all levels

### Member CRUD Operations

#### Create Member
```http
POST /api/members
Content-Type: application/json

{
  "firstName": "Fatima",
  "lastName": "Abdullah",
  "email": "fatima.abdullah@example.com",
  "phone": "+234987654321",
  "mkanid": 1002,
  "unitId": 5
}
```

#### Update Member
```http
PUT /api/members/{id}
Content-Type: application/json

{
  "firstName": "Fatima",
  "lastName": "Abdullah-Updated",
  "email": "fatima.updated@example.com",
  "phone": "+234987654321",
  "unitId": 5
}
```

#### Delete Member
```http
DELETE /api/members/{id}
```

### Advanced Member Queries

#### Get Complete Member Data
```http
GET /api/members/complete
```
**Description**: Returns members with full organizational structure and detailed role information including scope names.

---

## 🏢 Organization Management

### National Level

#### Get All Nationals
```http
GET /api/nationals
```

#### Get National by ID
```http
GET /api/nationals/{id}
```

### State Level

#### Get All States
```http
GET /api/states
```

#### Get State by ID
```http
GET /api/states/{id}
```

#### Create State
```http
POST /api/states
Content-Type: application/json

{
  "stateName": "Kano",
  "nationalId": 1
}
```

### Unit Level

#### Get All Units
```http
GET /api/units
```

#### Get Unit by ID
```http
GET /api/units/{id}
```

#### Get Units by State
```http
GET /api/units/state/{stateId}
```

#### Create Unit
```http
POST /api/units
Content-Type: application/json

{
  "unitName": "Ahmadu Bello University",
  "stateId": 15
}
```

#### Update Unit
```http
PUT /api/units/{id}
Content-Type: application/json

{
  "unitName": "Ahmadu Bello University (Updated)",
  "stateId": 15
}
```

#### Delete Unit
```http
DELETE /api/units/{id}
```

### Department Management

#### Get All Departments
```http
GET /api/departments
```

#### Get Department by ID
```http
GET /api/departments/{id}
```

#### Create Department
```http
POST /api/departments
Content-Type: application/json

{
  "departmentName": "Dawah"
}
```

#### Update Department
```http
PUT /api/departments/{id}
Content-Type: application/json

{
  "departmentName": "Dawah and Tabligh"
}
```

#### Delete Department
```http
DELETE /api/departments/{id}
```

### Level Management

#### Get All Levels
```http
GET /api/levels
```

#### Get Level Departments
```http
GET /api/levels/{id}/departments
```

#### Create Level
```http
POST /api/levels
Content-Type: application/json

{
  "levelType": "Vice President",
  "nationalId": 1,
  "stateId": null,
  "unitId": null
}
```

### Level Department Management

#### Get All Level Departments
```http
GET /api/leveldepartments
```

#### Get Level Departments by Scope
```http
GET /api/leveldepartments/scope/{scope}
```
**Scopes**: `national`, `state`, `unit`

#### Create Level Department
```http
POST /api/leveldepartments
Content-Type: application/json

{
  "levelId": 1,
  "departmentId": 5
}
```

#### Delete Level Department
```http
DELETE /api/leveldepartments/{id}
```

---

## 🔧 Role Assignment

### Assign EXCO Roles

#### Assign Role to Member
```http
POST /api/members/{memberId}/roles/{levelDepartmentId}
```

#### Remove Role from Member
```http
DELETE /api/members/{memberId}/roles/{levelDepartmentId}
```

#### Get Member Roles
```http
GET /api/members/{memberId}/roles
```
**Response**: 
```json
[
  {
    "memberLevelDepartmentId": 15,
    "levelDepartmentId": 8,
    "department": "Dawah",
    "level": "President",
    "scope": "National"
  }
]
```

#### Remove All Roles from Member
```http
DELETE /api/members/{memberId}/roles
```

---

## 📈 Statistics & Analytics

### Dashboard Statistics
```http
GET /api/stats/dashboard
```
**Response**:
```json
{
  "totalMembers": 1250,
  "totalUnits": 45,
  "totalDepartments": 12,
  "totalStates": 37,
  "totalNationals": 1,
  "totalLevels": 25,
  "excoMembers": 180,
  "nationalExcoCount": 15,
  "stateExcoCount": 85,
  "unitExcoCount": 80,
  "recentMembers": [
    {
      "firstName": "Ahmad",
      "lastName": "Kareem",
      "mkanid": 1001
    }
  ]
}
```

### Unit Statistics
```http
GET /api/stats/units
```

### Department Statistics
```http
GET /api/stats/departments
```
**Response**: Department performance with member counts across different organizational levels.

### Organization Summary
```http
GET /api/organization/summary
```
**Response**: Comprehensive overview including top-performing units and departments.

---

## 📥 Import & Export

### CSV Import Operations

#### Import EXCO from Default CSV
```http
GET /api/import/exco
```
**Description**: Imports from `excos_list_updated.csv` in the application root.

#### Upload and Import CSV
```http
POST /api/import/exco/upload
Content-Type: multipart/form-data

csvFile: [CSV FILE]
```

**CSV Format Requirements**:
```csv
NAME,UNIT,DEPARTMENT
Ahmad Kareem,University of Lagos,Dawah
Fatima Abdullah,Ahmadu Bello University,Education
```

#### Test CSV File Existence
```http
GET /api/import/test
```

**Import Features**:
- ✅ **Smart Name Matching**: Handles various name formats
- ✅ **Automatic Department Creation**: Creates missing departments
- ✅ **Duplicate Prevention**: Avoids duplicate role assignments
- ✅ **Detailed Error Reporting**: Returns unmatched records with reasons
- ✅ **National Level Targeting**: Specifically imports national-level executives

---

## 🔍 Advanced Queries

### Organizational Hierarchy
```http
GET /api/hierarchy
```
**Description**: Complete organizational tree from National → States → Units with member counts.

### Advanced Filtering

#### Members by Level Type and Scope
```http
GET /api/members/level/President
```

#### Level Departments by Organizational Scope
```http
GET /api/leveldepartments/scope/national
GET /api/leveldepartments/scope/state
GET /api/leveldepartments/scope/unit
```

---

## 💾 Bulk Operations

### Clear All EXCO Assignments
```http
DELETE /api/exco/clear
```
**⚠️ Warning**: This removes ALL executive role assignments system-wide.

### Bulk Role Removal
```http
DELETE /api/members/{memberId}/roles
```
**Description**: Removes all roles from a specific member.

---

## 🧪 Testing

### Interactive Test Interface

Access the built-in test interface at `/test.html` for:
- 🖱️ **Click-to-Test**: All endpoints with pre-configured buttons
- 📊 **Real-time Results**: Immediate response display
- 📁 **File Upload**: CSV import testing
- 🎯 **Custom Queries**: Manual endpoint testing
- ⚠️ **Bulk Operations**: Safe testing of destructive operations

### Example Test Scenarios

1. **Member Management Flow**:
   ```
   GET /api/members → POST /api/members → PUT /api/members/{id} → DELETE /api/members/{id}
   ```

2. **Role Assignment Flow**:
   ```
   GET /api/leveldepartments → POST /api/members/{id}/roles/{levelDeptId} → GET /api/members/{id}/roles
   ```

3. **Import Flow**:
   ```
   GET /api/import/test → POST /api/import/exco/upload → GET /api/stats/dashboard
   ```

---

## 🚀 Deployment

### Environment Configuration

**Development**:
```json
{
  "Environment": "Development",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AmsaDB;Trusted_Connection=true"
  }
}
```

**Production**:
```json
{
  "Environment": "Production",
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=AmsaDB;User=your-user;Password=your-password;Encrypt=true"
  }
}
```

### Features

- 🔄 **Auto Reference Cycle Handling**: Prevents JSON serialization errors
- 📝 **Comprehensive Validation**: Input validation with meaningful error messages
- 🔒 **Referential Integrity**: Maintains database consistency
- ⚡ **Optimized Queries**: Efficient EF Core query patterns
- 📊 **Rich Data Models**: Complete organizational hierarchy in responses
- 🛡️ **Error Handling**: Graceful error responses with detailed messages

### Performance Optimizations

- **Selective Loading**: Only load required data relationships
- **Projection Queries**: Use `Select()` to return only needed fields
- **Async Operations**: All database operations are asynchronous
- **Connection Pooling**: Efficient database connection management

---

## 📝 API Response Formats

### Success Response
```json
{
  "data": { /* response data */ },
  "status": 200
}
```

### Error Response
```json
{
  "error": "Detailed error message",
  "status": 400,
  "details": "Additional context if available"
}
```

### Common HTTP Status Codes

| Code | Description | Usage |
|------|-------------|-------|
| `200` | OK | Successful GET, PUT, DELETE |
| `201` | Created | Successful POST |
| `400` | Bad Request | Validation errors, invalid data |
| `404` | Not Found | Resource doesn't exist |
| `500` | Server Error | Internal server errors |

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## 🆘 Support

For support and questions:
- 📧 **Email**: [abdulawwalintisor777@gmail.com]
- 🐛 **Issues**: [GitHub Issues]
- 📖 **Documentation**: This README and inline API documentation

---

## Architecture Decision Records
This project uses Architecture Decision Records (ADRs) to document significant architectural decisions. See [ADR Index](docs/adr/adr-index.md) for a catalog of all decisions and [ADR Process](docs/adr/ADR-PROCESS.md) for guidelines on creating and managing ADRs.

---

**Built with ❤️ for AMSA Nigeria (Ahmadiyya Muslim Students Association)** 🇳🇬

<!-- Benchmarks removed from runtime build. See docs/adr for historical benchmark records. -->