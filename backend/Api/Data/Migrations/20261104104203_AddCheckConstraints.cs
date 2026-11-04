using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Series_Color",
                table: "Series",
                sql: "\"Color\" ~ '^#[0-9A-Fa-f]{6}$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Series_Name",
                table: "Series",
                sql: "char_length(btrim(\"Name\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Series_Range",
                table: "Series",
                sql: "\"MinValue\" < \"MaxValue\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sensors_Name",
                table: "Sensors",
                sql: "char_length(btrim(\"Name\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Measurements_Value_Finite",
                table: "Measurements",
                sql: "\"Value\" <> 'NaN'::float8 AND \"Value\" <> 'Infinity'::float8 AND \"Value\" <> '-Infinity'::float8");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Series_Color",
                table: "Series");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Series_Name",
                table: "Series");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Series_Range",
                table: "Series");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sensors_Name",
                table: "Sensors");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Measurements_Value_Finite",
                table: "Measurements");
        }
    }
}
