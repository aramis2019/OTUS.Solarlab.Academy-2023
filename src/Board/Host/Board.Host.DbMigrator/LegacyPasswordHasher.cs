using Board.Application.AppData.Contexts.Accounts.Services;
using Board.Domain.Account;
using Microsoft.EntityFrameworkCore;

namespace Board.Host.DbMigrator
{
    /// <summary>
    /// Хеширует пароли, оставшиеся в открытом виде с версий до появления хеширования.
    /// </summary>
    public static class LegacyPasswordHasher
    {
        /// <summary>
        /// Заменить открытые пароли на хеши. Повторный запуск ничего не меняет.
        /// </summary>
        public static async Task<int> HashPlainTextPasswordsAsync(MigrationDbContext context, IPasswordHasher hasher, CancellationToken cancellationToken = default)
        {
            var accounts = await context.Set<Account>().ToListAsync(cancellationToken);
            var updated = 0;
            foreach (var account in accounts.Where(a => !Pbkdf2PasswordHasher.IsHash(a.PasswordHash)))
            {
                account.PasswordHash = hasher.Hash(account.PasswordHash);
                updated++;
            }

            await context.SaveChangesAsync(cancellationToken);
            return updated;
        }
    }
}
