using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLMChat.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialVectorStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentVectors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ChunkId = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Embedding = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVectors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVectorEntity_DocumentId",
                table: "DocumentVectors",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVectorEntity_DocumentId_ChunkId",
                table: "DocumentVectors",
                columns: new[] { "DocumentId", "ChunkId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentVectors");
        }
    }
}
