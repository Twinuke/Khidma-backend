using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace khidma_backend.Migrations
{
    public partial class rogerakrtoro : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Contracts_ContractId",
                table: "Reviews");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Contracts_ContractId",
                table: "Reviews",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "ContractId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Contracts_ContractId",
                table: "Reviews");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Contracts_ContractId",
                table: "Reviews",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "ContractId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
