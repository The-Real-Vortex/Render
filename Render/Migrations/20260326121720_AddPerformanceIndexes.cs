using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Render.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_UserId",
                table: "Posts");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CreatedAt",
                table: "Posts",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_LikesCount",
                table: "Posts",
                column: "LikesCount",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_UserId_CreatedAt",
                table: "Posts",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Likes_UserId",
                table: "Likes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_CreatedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_LikesCount",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_UserId_CreatedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Likes_UserId",
                table: "Likes");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_UserId",
                table: "Posts",
                column: "UserId");
        }
    }
}
