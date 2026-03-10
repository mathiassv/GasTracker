using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GasTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCostDistanceUnitDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE AppUsers SET CostDistanceUnit = '10km' WHERE CostDistanceUnit = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
