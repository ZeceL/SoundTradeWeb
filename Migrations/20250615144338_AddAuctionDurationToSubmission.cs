using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoundTradeWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionDurationToSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuctionDurationInMinutes",
                table: "AuctionSubmissions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuctionDurationInMinutes",
                table: "AuctionSubmissions");
        }
    }
}
