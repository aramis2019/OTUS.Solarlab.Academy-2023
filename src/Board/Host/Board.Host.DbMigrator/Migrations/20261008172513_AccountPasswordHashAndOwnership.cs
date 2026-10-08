using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board.Host.DbMigrator.Migrations
{
    /// <inheritdoc />
    public partial class AccountPasswordHashAndOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Колонку переименовываем, а не пересоздаём: существующие пароли хеширует
            // Board.Host.DbMigrator после применения миграций (см. LegacyPasswordHasher).
            migrationBuilder.RenameColumn(
                name: "Password",
                table: "Account",
                newName: "PasswordHash");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Account",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "File",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "Advert",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_File_AccountId",
                table: "File",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Advert_AccountId",
                table: "Advert",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Account_Login",
                table: "Account",
                column: "Login",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Advert_Account_AccountId",
                table: "Advert",
                column: "AccountId",
                principalTable: "Account",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_File_Account_AccountId",
                table: "File",
                column: "AccountId",
                principalTable: "Account",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Advert_Account_AccountId",
                table: "Advert");

            migrationBuilder.DropForeignKey(
                name: "FK_File_Account_AccountId",
                table: "File");

            migrationBuilder.DropIndex(
                name: "IX_File_AccountId",
                table: "File");

            migrationBuilder.DropIndex(
                name: "IX_Advert_AccountId",
                table: "Advert");

            migrationBuilder.DropIndex(
                name: "IX_Account_Login",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "File");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Advert");

            // Хеши не помещаются в старую колонку и не могут быть преобразованы обратно в пароли.
            migrationBuilder.Sql("UPDATE \"Account\" SET \"PasswordHash\" = '';");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Account",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "Account",
                newName: "Password");
        }
    }
}
