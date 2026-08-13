using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace uni_schedule_builder.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyGroupsAndTermGroupLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceId",
                table: "RegularClassTerms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudyGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegularClassTermGroups",
                columns: table => new
                {
                    RegularClassTermId = table.Column<int>(type: "INTEGER", nullable: false),
                    StudyGroupId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegularClassTermGroups", x => new { x.RegularClassTermId, x.StudyGroupId });
                    table.ForeignKey(
                        name: "FK_RegularClassTermGroups_RegularClassTerms_RegularClassTermId",
                        column: x => x.RegularClassTermId,
                        principalTable: "RegularClassTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegularClassTermGroups_StudyGroups_StudyGroupId",
                        column: x => x.StudyGroupId,
                        principalTable: "StudyGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegularClassTerms_SourceId",
                table: "RegularClassTerms",
                column: "SourceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegularClassTermGroups_StudyGroupId",
                table: "RegularClassTermGroups",
                column: "StudyGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyGroups_Name",
                table: "StudyGroups",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegularClassTermGroups");

            migrationBuilder.DropTable(
                name: "StudyGroups");

            migrationBuilder.DropIndex(
                name: "IX_RegularClassTerms_SourceId",
                table: "RegularClassTerms");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "RegularClassTerms");
        }
    }
}
