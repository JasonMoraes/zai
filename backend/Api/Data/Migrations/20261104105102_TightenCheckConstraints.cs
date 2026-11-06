using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TightenCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Series_Name",
                table: "Series");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sensors_Name",
                table: "Sensors");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Series_Finite",
                table: "Series",
                sql: "\"MinValue\" <> 'NaN'::float8 AND \"MinValue\" <> 'Infinity'::float8 AND \"MinValue\" <> '-Infinity'::float8 AND \"MaxValue\" <> 'NaN'::float8 AND \"MaxValue\" <> 'Infinity'::float8 AND \"MaxValue\" <> '-Infinity'::float8");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Series_Name",
                table: "Series",
                sql: "\"Name\" !~ '^[[:space:]]*$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sensors_Name",
                table: "Sensors",
                sql: "\"Name\" !~ '^[[:space:]]*$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Series_Finite",
                table: "Series");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Series_Name",
                table: "Series");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sensors_Name",
                table: "Sensors");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Series_Name",
                table: "Series",
                sql: "char_length(btrim(\"Name\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sensors_Name",
                table: "Sensors",
                sql: "char_length(btrim(\"Name\")) > 0");
        }
    }
}
