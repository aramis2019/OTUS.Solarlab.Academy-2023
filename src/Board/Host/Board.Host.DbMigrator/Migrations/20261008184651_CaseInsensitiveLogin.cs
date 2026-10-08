using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board.Host.DbMigrator.Migrations
{
    /// <inheritdoc />
    public partial class CaseInsensitiveLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Account_Login",
                table: "Account");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedLogin",
                table: "Account",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Заполняем так же, как Account.NormalizeLogin: Trim + верхний регистр.
            migrationBuilder.Sql(@"UPDATE ""Account"" SET ""NormalizedLogin"" = UPPER(TRIM(""Login""));");

            // Если есть логины, совпадающие без учёта регистра, уникальный индекс не создастся.
            // Падаем с понятным сообщением: такие аккаунты нужно переименовать вручную.
            migrationBuilder.Sql(@"
DO $$
DECLARE duplicates text;
BEGIN
    SELECT string_agg(""NormalizedLogin"", ', ') INTO duplicates
    FROM (SELECT ""NormalizedLogin"" FROM ""Account"" GROUP BY ""NormalizedLogin"" HAVING COUNT(*) > 1) d;
    IF duplicates IS NOT NULL THEN
        RAISE EXCEPTION 'Логины совпадают без учёта регистра: %. Переименуйте аккаунты и повторите миграцию.', duplicates;
    END IF;
END $$;");

            migrationBuilder.CreateIndex(
                name: "IX_Account_NormalizedLogin",
                table: "Account",
                column: "NormalizedLogin",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Account_NormalizedLogin",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "NormalizedLogin",
                table: "Account");

            migrationBuilder.CreateIndex(
                name: "IX_Account_Login",
                table: "Account",
                column: "Login",
                unique: true);
        }
    }
}
