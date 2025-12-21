using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace khidma_backend.Migrations
{
    public partial class roroakakaka : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserConnections_Users_TargetId",
                table: "UserConnections");

            migrationBuilder.RenameColumn(
                name: "TargetId",
                table: "UserConnections",
                newName: "ReceiverId");

            migrationBuilder.RenameIndex(
                name: "IX_UserConnections_TargetId",
                table: "UserConnections",
                newName: "IX_UserConnections_ReceiverId");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "UserConnections",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_UserConnections_Users_ReceiverId",
                table: "UserConnections",
                column: "ReceiverId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserConnections_Users_ReceiverId",
                table: "UserConnections");

            migrationBuilder.RenameColumn(
                name: "ReceiverId",
                table: "UserConnections",
                newName: "TargetId");

            migrationBuilder.RenameIndex(
                name: "IX_UserConnections_ReceiverId",
                table: "UserConnections",
                newName: "IX_UserConnections_TargetId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "UserConnections",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_UserConnections_Users_TargetId",
                table: "UserConnections",
                column: "TargetId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
