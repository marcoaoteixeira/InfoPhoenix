using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nameless.InfoPhoenix.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentDirectories",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    DirectoryPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    LastIndexingTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentDirectories", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentDirectoryID = table.Column<Guid>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 2147483647, nullable: false),
                    RawFile = table.Column<byte[]>(type: "BLOB", nullable: false),
                    LastIndexingTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastWriteTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Documents_DocumentDirectories_DocumentDirectoryID",
                        column: x => x.DocumentDirectoryID,
                        principalTable: "DocumentDirectories",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDirectories_DirectoryPath",
                table: "DocumentDirectories",
                column: "DirectoryPath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentDirectoryID",
                table: "Documents",
                column: "DocumentDirectoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FilePath",
                table: "Documents",
                column: "FilePath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "DocumentDirectories");
        }
    }
}
