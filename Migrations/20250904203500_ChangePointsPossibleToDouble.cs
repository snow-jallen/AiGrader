using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiGrader.Migrations
{
    /// <inheritdoc />
    public partial class ChangePointsPossibleToDouble : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite doesn't support ALTER COLUMN, so we need to recreate the table
            migrationBuilder.Sql(@"
                CREATE TABLE ""Assignments_temp"" AS SELECT 
                    ""Id"", ""Name"", ""CourseId"", ""DueAt"", ""Published"", 
                    CAST(""PointsPossible"" AS REAL) AS ""PointsPossible"",
                    ""UngradedCount"", ""TotalSubmissions"", ""LastSynced"", 
                    ""HasDownloadedSubmissions"", ""LocalSubmissionsPath""
                FROM ""Assignments"";
                
                DROP TABLE ""Assignments"";
                
                ALTER TABLE ""Assignments_temp"" RENAME TO ""Assignments"";
                
                CREATE INDEX ""IX_Assignments_CourseId"" ON ""Assignments"" (""CourseId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: change back to INTEGER
            migrationBuilder.Sql(@"
                CREATE TABLE ""Assignments_temp"" AS SELECT 
                    ""Id"", ""Name"", ""CourseId"", ""DueAt"", ""Published"", 
                    CAST(""PointsPossible"" AS INTEGER) AS ""PointsPossible"",
                    ""UngradedCount"", ""TotalSubmissions"", ""LastSynced"", 
                    ""HasDownloadedSubmissions"", ""LocalSubmissionsPath""
                FROM ""Assignments"";
                
                DROP TABLE ""Assignments"";
                
                ALTER TABLE ""Assignments_temp"" RENAME TO ""Assignments"";
                
                CREATE INDEX ""IX_Assignments_CourseId"" ON ""Assignments"" (""CourseId"");
            ");
        }
    }
}