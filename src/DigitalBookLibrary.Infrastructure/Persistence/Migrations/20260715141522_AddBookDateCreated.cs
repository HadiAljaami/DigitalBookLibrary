using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalBookLibrary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookDateCreated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "Books",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Books that predate this column would otherwise read as year 0001 and sort ahead of
            // everything else in the recent-books feed. Their true add-date is unknowable, so stamp
            // them with the migration time — off by a little, rather than nonsense.
            migrationBuilder.Sql(
                "UPDATE Books SET DateCreated = GETUTCDATE() WHERE DateCreated = '0001-01-01T00:00:00';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "Books");
        }
    }
}
