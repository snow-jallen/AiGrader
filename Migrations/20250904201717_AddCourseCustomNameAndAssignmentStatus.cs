using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiGrader.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseCustomNameAndAssignmentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomName",
                table: "Courses",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueAt",
                table: "Assignments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PointsPossible",
                table: "Assignments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Published",
                table: "Assignments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TotalSubmissions",
                table: "Assignments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UngradedCount",
                table: "Assignments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomName",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "PointsPossible",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "Published",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "TotalSubmissions",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "UngradedCount",
                table: "Assignments");
        }
    }
}
