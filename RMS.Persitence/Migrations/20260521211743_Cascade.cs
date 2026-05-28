using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMS.Persitence.Migrations
{
    /// <inheritdoc />
    public partial class Cascade : Migration
    {
        
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_Employees_AspNetUsers_AppUserId",
                    table: "Employees");

                migrationBuilder.AddForeignKey(
                    name: "FK_Employees_AspNetUsers_AppUserId",
                    table: "Employees",
                    column: "AppUserId",
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            }

            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_Employees_AspNetUsers_AppUserId",
                    table: "Employees");

                migrationBuilder.AddForeignKey(
                    name: "FK_Employees_AspNetUsers_AppUserId",
                    table: "Employees",
                    column: "AppUserId",
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            }
        }
    }

