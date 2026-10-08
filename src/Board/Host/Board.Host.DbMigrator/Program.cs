using Board.Application.AppData.Contexts.Accounts.Services;
using Microsoft.EntityFrameworkCore;

namespace Board.Host.DbMigrator
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args).ConfigureServices((hostContext, services) =>
            {
                services.AddServices(hostContext.Configuration);
            }).Build();
            await MigrateDatabaseAsync(host.Services);
            await host.RunAsync();
        }

        private static async Task MigrateDatabaseAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MigrationDbContext>();
            await context.Database.MigrateAsync();

            var hashed = await LegacyPasswordHasher.HashPlainTextPasswordsAsync(context, new Pbkdf2PasswordHasher());
            scope.ServiceProvider.GetRequiredService<ILogger<Program>>()
                .LogInformation("Захешировано паролей, хранившихся в открытом виде: {Count}", hashed);
        }
    }
}