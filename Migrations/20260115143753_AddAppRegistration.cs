using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmsaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAppRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppRegistrations",
                columns: table => new
                {
                    AppId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AppName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AppSecretHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AllowedScopes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokenExpirationHours = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AppRegistrations__AppId", x => x.AppId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppRegistrations");
        }
    }
}
