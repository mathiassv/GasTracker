using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPartialFillUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPartialFillUp",
                table: "FuelLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPartialFillUp",
                table: "FuelLogs");
        }
    }
}
