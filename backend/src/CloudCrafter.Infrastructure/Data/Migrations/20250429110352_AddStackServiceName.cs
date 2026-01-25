using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudCrafter.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStackServiceName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DockerComposeData_ServiceName",
                table: "StackServices",
                type: "text",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DockerComposeData_ServiceName",
                table: "StackServices"
            );
        }
    }
}
