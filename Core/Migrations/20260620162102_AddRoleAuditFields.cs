using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PermitPro.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Description",
                table: "AspNetRoles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "AspNetRoles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedWhen",
                table: "AspNetRoles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "AspNetRoles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedWhen",
                table: "AspNetRoles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AspNetRoles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemRole",
                table: "AspNetRoles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "AspNetRoles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedWhen",
                table: "AspNetRoles",
                type: "datetime2",
                nullable: true);

            // Seed all existing roles as system roles with the required audit values.
            migrationBuilder.Sql(@"
                UPDATE [AspNetRoles]
                SET
                    [IsSystemRole] = 1,
                    [CreatedBy]   = '25b7d2f5-29e7-45ca-8aef-a760b7dd4c7d',
                    [CreatedWhen] = '2025-01-01 00:00:00.0000000'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "CreatedWhen",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "DeletedWhen",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "IsSystemRole",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedWhen",
                table: "AspNetRoles");
        }
    }
}
