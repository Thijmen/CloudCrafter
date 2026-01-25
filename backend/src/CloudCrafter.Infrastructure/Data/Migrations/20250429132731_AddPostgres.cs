using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudCrafter.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "StackServiceTypes",
                columns: new[] { "Id", "Type" },
                values: new object[]
                {
                    new Guid("81fc7fa9-a1ef-4ddc-b181-6f019ff0568b"),
                    "DatabasePostgres",
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "StackServiceTypes",
                keyColumn: "Id",
                keyValue: new Guid("81fc7fa9-a1ef-4ddc-b181-6f019ff0568b")
            );
        }
    }
}
