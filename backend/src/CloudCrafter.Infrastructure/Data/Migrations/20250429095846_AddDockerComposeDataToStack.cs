using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudCrafter.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDockerComposeDataToStack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DockerComposeData_DockerComposeFile",
                table: "Stacks",
                type: "text",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DockerComposeData_DockerComposeFile",
                table: "Stacks"
            );
        }
    }
}
