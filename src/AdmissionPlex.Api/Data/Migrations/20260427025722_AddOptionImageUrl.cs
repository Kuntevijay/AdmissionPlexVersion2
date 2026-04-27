using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdmissionPlex.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOptionImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "question_options",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "question_options");
        }
    }
}
