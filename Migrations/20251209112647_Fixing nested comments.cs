using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace khidma_backend.Migrations
{
    public partial class Fixingnestedcomments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentCommentId",
                table: "PostComments",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentCommentId",
                table: "PostComments");
        }
    }
}
