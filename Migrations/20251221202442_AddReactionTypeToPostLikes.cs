using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace khidma_backend.Migrations
{
    public partial class AddReactionTypeToPostLikes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReactionType",
                table: "PostLikes",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReactionType",
                table: "PostLikes");
        }
    }
}
