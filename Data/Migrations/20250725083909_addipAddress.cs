using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JustAskIndia.Data.Migrations
{
    /// <inheritdoc />
    public partial class addipAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "UserLogins",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "UserLogins");
        }
    }
}
