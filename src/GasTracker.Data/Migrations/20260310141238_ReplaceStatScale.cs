using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceStatScale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostDistanceUnit",
                table: "AppUsers");

            migrationBuilder.AddColumn<int>(
                name: "StatScale",
                table: "AppUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatScale",
                table: "AppUsers");

            migrationBuilder.AddColumn<string>(
                name: "CostDistanceUnit",
                table: "AppUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
