using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Walls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Length = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: false),
                    Height = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: false),
                    Thickness = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: false),
                    AssemblyType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AssemblyDetails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RValue = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: true),
                    UValue = table.Column<double>(type: "float(10)", precision: 10, scale: 4, nullable: true),
                    MaterialLayers = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Orientation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Walls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Windows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Width = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: false),
                    Height = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: false),
                    Area = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: false),
                    FrameType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FrameDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GlazingType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GlazingDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UValue = table.Column<double>(type: "float(10)", precision: 10, scale: 4, nullable: true),
                    SolarHeatGainCoefficient = table.Column<double>(type: "float(5)", precision: 5, scale: 3, nullable: true),
                    VisibleTransmittance = table.Column<double>(type: "float(5)", precision: 5, scale: 3, nullable: true),
                    AirLeakage = table.Column<double>(type: "float(10)", precision: 10, scale: 4, nullable: true),
                    EnergyStarRating = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NFRCRating = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Orientation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InstallationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OperationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HasScreens = table.Column<bool>(type: "bit", nullable: true),
                    HasStormWindows = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Windows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonRoles",
                columns: table => new
                {
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonRoles", x => new { x.PersonId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_PersonRoles_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonRoles_RoleId",
                table: "PersonRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonRoles");

            migrationBuilder.DropTable(
                name: "Walls");

            migrationBuilder.DropTable(
                name: "Windows");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
