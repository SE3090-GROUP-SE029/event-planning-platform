using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(Infrastructure.Data.AppDbContext))]
    [Migration("20261005145315_AddProtectedRegistrationStatusSecret")]
    public partial class AddProtectedRegistrationStatusSecret : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProtectedStatusSecret",
                table: "RegistrationSubmissions",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProtectedStatusSecret",
                table: "RegistrationSubmissions");
        }
    }
}
