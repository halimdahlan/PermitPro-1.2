using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PermitPro.Core.Migrations
{
	/// <inheritdoc />
	public partial class AddedNewPropertiesPermit01 : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<DateTime>(
				 name: "ApprovedDateTime",
				 table: "Permits",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "RejectedDateTime",
				 table: "Permits",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "ResumedDateTime",
				 table: "Permits",
				 type: "datetime2",
				 nullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				 name: "ApprovedDateTime",
				 table: "Permits");

			migrationBuilder.DropColumn(
				 name: "RejectedDateTime",
				 table: "Permits");

			migrationBuilder.DropColumn(
				 name: "ResumedDateTime",
				 table: "Permits");
		}
	}
}
