using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmsaAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Departme__B2079BEDCF4DB986", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "National",
                columns: table => new
                {
                    NationalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NationalName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__National__E9AA32FB057063F4", x => x.NationalId);
                });

            migrationBuilder.CreateTable(
                name: "States",
                columns: table => new
                {
                    StateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NationalId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__States__C3BA3B3A5778F3FA", x => x.StateId);
                    table.ForeignKey(
                        name: "FK__States__National__60A75C0F",
                        column: x => x.NationalId,
                        principalTable: "National",
                        principalColumn: "NationalId");
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    UnitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StateId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Units__44F5ECB508D31CE9", x => x.UnitId);
                    table.ForeignKey(
                        name: "FK__Units__StateId__619B8048",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "StateId");
                });

            migrationBuilder.CreateTable(
                name: "Levels",
                columns: table => new
                {
                    LevelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LevelType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    NationalId = table.Column<int>(type: "int", nullable: true),
                    StateId = table.Column<int>(type: "int", nullable: true),
                    UnitId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Levels", x => x.LevelId);
                    table.ForeignKey(
                        name: "FK_Levels_National",
                        column: x => x.NationalId,
                        principalTable: "National",
                        principalColumn: "NationalId");
                    table.ForeignKey(
                        name: "FK_Levels_States",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "StateId");
                    table.ForeignKey(
                        name: "FK_Levels_Units",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "UnitId");
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    MemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    MKANID = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tmp_ms_x__0CF04B18844A1307", x => x.MemberId);
                    table.ForeignKey(
                        name: "FK__Members__UnitId__0C85DE4D",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "UnitId");
                });

            migrationBuilder.CreateTable(
                name: "LevelDepartments",
                columns: table => new
                {
                    LevelDepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LevelId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__LevelDep__70EB3E688D7838FC", x => x.LevelDepartmentId);
                    table.ForeignKey(
                        name: "FK_LevelDepartments_Levels",
                        column: x => x.LevelId,
                        principalTable: "Levels",
                        principalColumn: "LevelId");
                    table.ForeignKey(
                        name: "FK__LevelDepa__Depar__6FE99F9F",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId");
                });

            migrationBuilder.CreateTable(
                name: "MemberLevelDepartments",
                columns: table => new
                {
                    MemberLevelDepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    LevelDepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__MemberLe__C4CEBAB85CF42426", x => x.MemberLevelDepartmentId);
                    table.ForeignKey(
                        name: "FK__MemberLev__Level__71D1E811",
                        column: x => x.LevelDepartmentId,
                        principalTable: "LevelDepartments",
                        principalColumn: "LevelDepartmentId");
                    table.ForeignKey(
                        name: "FK__MemberLev__Membe__0B91BA14",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "MemberId");
                });

            migrationBuilder.CreateIndex(
                name: "UQ__Departme__D949CC34646C8B5B",
                table: "Departments",
                column: "DepartmentName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LevelDepartments_DepartmentId",
                table: "LevelDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_LevelDepartments_LevelId",
                table: "LevelDepartments",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Levels_NationalId",
                table: "Levels",
                column: "NationalId");

            migrationBuilder.CreateIndex(
                name: "IX_Levels_StateId",
                table: "Levels",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_Levels_UnitId",
                table: "Levels",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "UQ_Levels_LevelType_Id",
                table: "Levels",
                columns: new[] { "LevelType", "NationalId", "StateId", "UnitId" },
                unique: true,
                filter: "[NationalId] IS NOT NULL AND [StateId] IS NOT NULL AND [UnitId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MemberLevelDepartments_LevelDepartmentId",
                table: "MemberLevelDepartments",
                column: "LevelDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberLevelDepartments_MemberId",
                table: "MemberLevelDepartments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_UnitId",
                table: "Members",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "UQ__tmp_ms_x__5C7E359E3AD99E1D",
                table: "Members",
                column: "Phone",
                unique: true,
                filter: "[Phone] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ__tmp_ms_x__706BAAA967D044CC",
                table: "Members",
                column: "MKANID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__tmp_ms_x__A9D10534C9D85A05",
                table: "Members",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ__National__1D8A5E87EDFF4DF5",
                table: "National",
                column: "NationalName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_States_NationalId",
                table: "States",
                column: "NationalId");

            migrationBuilder.CreateIndex(
                name: "UQ__States__554763159073B91B",
                table: "States",
                column: "StateName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_StateId",
                table: "Units",
                column: "StateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberLevelDepartments");

            migrationBuilder.DropTable(
                name: "LevelDepartments");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "Levels");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "States");

            migrationBuilder.DropTable(
                name: "National");
        }
    }
}
