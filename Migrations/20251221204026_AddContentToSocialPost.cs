using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace khidma_backend.Migrations
{
    public partial class AddContentToSocialPost : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "SocialPosts",
                type: "TEXT",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "SocialPosts");
        }
    }
}
