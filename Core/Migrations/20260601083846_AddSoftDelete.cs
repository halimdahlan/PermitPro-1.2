using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PermitPro.Core.Migrations
{
	/// <inheritdoc />
	public partial class AddSoftDelete : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<Guid>(
				 name: "DeletedBy",
				 table: "WorkflowSteps",
				 type: "uniqueidentifier",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "DeletedWhen",
				 table: "WorkflowSteps",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<bool>(
				 name: "IsDeleted",
				 table: "WorkflowSteps",
				 type: "bit",
				 nullable: false,
				 defaultValue: false);

			migrationBuilder.AddColumn<Guid>(
				 name: "DeletedBy",
				 table: "Workflows",
				 type: "uniqueidentifier",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "DeletedWhen",
				 table: "Workflows",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<bool>(
				 name: "IsDeleted",
				 table: "Workflows",
				 type: "bit",
				 nullable: false,
				 defaultValue: false);

			migrationBuilder.AddColumn<Guid>(
				 name: "DeletedBy",
				 table: "WorkflowHistories",
				 type: "uniqueidentifier",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "DeletedWhen",
				 table: "WorkflowHistories",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<bool>(
				 name: "IsDeleted",
				 table: "WorkflowHistories",
				 type: "bit",
				 nullable: false,
				 defaultValue: false);

			migrationBuilder.AddColumn<Guid>(
				 name: "DeletedBy",
				 table: "SystemMenus",
				 type: "uniqueidentifier",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "DeletedWhen",
				 table: "SystemMenus",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<bool>(
				 name: "IsDeleted",
				 table: "SystemMenus",
				 type: "bit",
				 nullable: false,
				 defaultValue: false);

			migrationBuilder.AddColumn<Guid>(
				 name: "DeletedBy",
				 table: "Sites",
				 type: "uniqueidentifier",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "DeletedWhen",
				 table: "Sites",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<bool>(
				 name: "IsDeleted",
				 table: "Sites",
				 type: "bit",
				 nullable: false,
				 defaultValue: false);

			migrationBuilder.AddColumn<Guid>(
				 name: "DeletedBy",
				 table: "Permits",
				 type: "uniqueidentifier",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "DeletedWhen",
				 table: "Permits",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<bool>(
				 name: "IsDeleted",
				 table: "Permits",
				 type: "bit",
				 nullable: false,
				 defaultValue: false);

			migrationBuilder.AddColumn<Guid>(
				 name: "DeletedBy",
				 table: "Notifications",
				 type: "uniqueidentifier",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "DeletedWhen",
				 table: "Notifications",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<bool>(
				 name: "IsDeleted",
				 table: "Notifications",
				 type: "bit",
				 nullable: false,
				 defaultValue: false);

			migrationBuilder.AddColumn<Guid>(
				 name: "DeletedBy",
				 table: "Divisions",
				 type: "uniqueidentifier",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "DeletedWhen",
				 table: "Divisions",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<bool>(
				 name: "IsDeleted",
				 table: "Divisions",
				 type: "bit",
				 nullable: false,
				 defaultValue: false);

			migrationBuilder.AddColumn<Guid>(
				 name: "DeletedBy",
				 table: "Departments",
				 type: "uniqueidentifier",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "DeletedWhen",
				 table: "Departments",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<bool>(
				 name: "IsDeleted",
				 table: "Departments",
				 type: "bit",
				 nullable: false,
				 defaultValue: false);

			migrationBuilder.AddColumn<Guid>(
				 name: "DeletedBy",
				 table: "Contacts",
				 type: "uniqueidentifier",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "DeletedWhen",
				 table: "Contacts",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<bool>(
				 name: "IsDeleted",
				 table: "Contacts",
				 type: "bit",
				 nullable: false,
				 defaultValue: false);

			migrationBuilder.AddColumn<Guid>(
				 name: "DeletedBy",
				 table: "Companies",
				 type: "uniqueidentifier",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "DeletedWhen",
				 table: "Companies",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<bool>(
				 name: "IsDeleted",
				 table: "Companies",
				 type: "bit",
				 nullable: false,
				 defaultValue: false);

			migrationBuilder.AddColumn<Guid>(
				 name: "DeletedBy",
				 table: "Certificates",
				 type: "uniqueidentifier",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "DeletedWhen",
				 table: "Certificates",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<bool>(
				 name: "IsDeleted",
				 table: "Certificates",
				 type: "bit",
				 nullable: false,
				 defaultValue: false);

			migrationBuilder.AddColumn<Guid>(
				 name: "DeletedBy",
				 table: "Attachments",
				 type: "uniqueidentifier",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "DeletedWhen",
				 table: "Attachments",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<bool>(
				 name: "IsDeleted",
				 table: "Attachments",
				 type: "bit",
				 nullable: false,
				 defaultValue: false);

			migrationBuilder.AddColumn<Guid>(
				 name: "DeletedBy",
				 table: "AspNetUsers",
				 type: "uniqueidentifier",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "DeletedWhen",
				 table: "AspNetUsers",
				 type: "datetime2",
				 nullable: true);

			//migrationBuilder.AddColumn<bool>(
			//    name: "IsDeleted",
			//    table: "AspNetUsers",
			//    type: "bit",
			//    nullable: false,
			//    defaultValue: false);

			migrationBuilder.AddColumn<Guid>(
				 name: "DeletedBy",
				 table: "Addresses",
				 type: "uniqueidentifier",
				 nullable: true);

			migrationBuilder.AddColumn<DateTime>(
				 name: "DeletedWhen",
				 table: "Addresses",
				 type: "datetime2",
				 nullable: true);

			migrationBuilder.AddColumn<bool>(
				 name: "IsDeleted",
				 table: "Addresses",
				 type: "bit",
				 nullable: false,
				 defaultValue: false);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				 name: "DeletedBy",
				 table: "WorkflowSteps");

			migrationBuilder.DropColumn(
				 name: "DeletedWhen",
				 table: "WorkflowSteps");

			migrationBuilder.DropColumn(
				 name: "IsDeleted",
				 table: "WorkflowSteps");

			migrationBuilder.DropColumn(
				 name: "DeletedBy",
				 table: "Workflows");

			migrationBuilder.DropColumn(
				 name: "DeletedWhen",
				 table: "Workflows");

			migrationBuilder.DropColumn(
				 name: "IsDeleted",
				 table: "Workflows");

			migrationBuilder.DropColumn(
				 name: "DeletedBy",
				 table: "WorkflowHistories");

			migrationBuilder.DropColumn(
				 name: "DeletedWhen",
				 table: "WorkflowHistories");

			migrationBuilder.DropColumn(
				 name: "IsDeleted",
				 table: "WorkflowHistories");

			migrationBuilder.DropColumn(
				 name: "DeletedBy",
				 table: "SystemMenus");

			migrationBuilder.DropColumn(
				 name: "DeletedWhen",
				 table: "SystemMenus");

			migrationBuilder.DropColumn(
				 name: "IsDeleted",
				 table: "SystemMenus");

			migrationBuilder.DropColumn(
				 name: "DeletedBy",
				 table: "Sites");

			migrationBuilder.DropColumn(
				 name: "DeletedWhen",
				 table: "Sites");

			migrationBuilder.DropColumn(
				 name: "IsDeleted",
				 table: "Sites");

			migrationBuilder.DropColumn(
				 name: "DeletedBy",
				 table: "Permits");

			migrationBuilder.DropColumn(
				 name: "DeletedWhen",
				 table: "Permits");

			migrationBuilder.DropColumn(
				 name: "IsDeleted",
				 table: "Permits");

			migrationBuilder.DropColumn(
				 name: "DeletedBy",
				 table: "Notifications");

			migrationBuilder.DropColumn(
				 name: "DeletedWhen",
				 table: "Notifications");

			migrationBuilder.DropColumn(
				 name: "IsDeleted",
				 table: "Notifications");

			migrationBuilder.DropColumn(
				 name: "DeletedBy",
				 table: "Divisions");

			migrationBuilder.DropColumn(
				 name: "DeletedWhen",
				 table: "Divisions");

			migrationBuilder.DropColumn(
				 name: "IsDeleted",
				 table: "Divisions");

			migrationBuilder.DropColumn(
				 name: "DeletedBy",
				 table: "Departments");

			migrationBuilder.DropColumn(
				 name: "DeletedWhen",
				 table: "Departments");

			migrationBuilder.DropColumn(
				 name: "IsDeleted",
				 table: "Departments");

			migrationBuilder.DropColumn(
				 name: "DeletedBy",
				 table: "Contacts");

			migrationBuilder.DropColumn(
				 name: "DeletedWhen",
				 table: "Contacts");

			migrationBuilder.DropColumn(
				 name: "IsDeleted",
				 table: "Contacts");

			migrationBuilder.DropColumn(
				 name: "DeletedBy",
				 table: "Companies");

			migrationBuilder.DropColumn(
				 name: "DeletedWhen",
				 table: "Companies");

			migrationBuilder.DropColumn(
				 name: "IsDeleted",
				 table: "Companies");

			migrationBuilder.DropColumn(
				 name: "DeletedBy",
				 table: "Certificates");

			migrationBuilder.DropColumn(
				 name: "DeletedWhen",
				 table: "Certificates");

			migrationBuilder.DropColumn(
				 name: "IsDeleted",
				 table: "Certificates");

			migrationBuilder.DropColumn(
				 name: "DeletedBy",
				 table: "Attachments");

			migrationBuilder.DropColumn(
				 name: "DeletedWhen",
				 table: "Attachments");

			migrationBuilder.DropColumn(
				 name: "IsDeleted",
				 table: "Attachments");

			migrationBuilder.DropColumn(
				 name: "DeletedBy",
				 table: "AspNetUsers");

			migrationBuilder.DropColumn(
				 name: "DeletedWhen",
				 table: "AspNetUsers");

			migrationBuilder.DropColumn(
				 name: "IsDeleted",
				 table: "AspNetUsers");

			migrationBuilder.DropColumn(
				 name: "DeletedBy",
				 table: "Addresses");

			migrationBuilder.DropColumn(
				 name: "DeletedWhen",
				 table: "Addresses");

			migrationBuilder.DropColumn(
				 name: "IsDeleted",
				 table: "Addresses");
		}
	}
}
