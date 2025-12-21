using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace khidma_backend.Migrations
{
    public partial class Migratio : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Column already exists in DB, so do nothing.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Do nothing (avoid dropping an existing column unexpectedly).
        }
    }
}
