using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCostDistanceUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CostDistanceUnit",
                table: "AppUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostDistanceUnit",
                table: "AppUsers");
        }
    }
}
