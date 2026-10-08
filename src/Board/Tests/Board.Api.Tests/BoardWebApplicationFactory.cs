using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Board.Contracts.Account;
using Board.Infrastucture.DataAccess;
using Board.Infrastucture.DataAccess.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Board.Api.Tests
{
    /// <summary>
    /// Фабрика тестого варианта сервиса.
    /// </summary>
    public class BoardWebApplicationFactory : WebApplicationFactory<Program>
    {
        public const string TestJwtKey = "test-jwt-signing-key-at-least-32-bytes-long";
        public const string AdminLogin = "test_admin";
        private const string TestPassword = "P@ssw0rd!";

        // Своя БД на каждую фабрику: тестовые классы выполняются параллельно и не должны видеть данные друг друга.
        private readonly string _databaseName = $"BoardDb_{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Jwt:Key", TestJwtKey);
            // Тесты регистрируют много пользователей с одного «адреса»; сам лимит проверяется отдельным тестом.
            builder.UseSetting("RateLimiting:Auth:PermitLimit", "100000");
            builder.UseSetting("Administration:AdminLogins:0", AdminLogin);

            builder.ConfigureServices(services =>
            {
                var descriptor =
                    services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextOptionsConfigurator<BoardDbContext>));

                services.Remove(descriptor!);

                services.AddSingleton<IDbContextOptionsConfigurator<BoardDbContext>>(sp =>
                    new TestBoardDbContextConfiguration(_databaseName, sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()));
                
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<BoardDbContext>();

                db.Database.EnsureCreated();
                DataSeedHelper.InitializeDbForTests(db);
            });
        }

        /// <summary>
        /// Зарегистрировать нового пользователя и получить клиент с его JWT.
        /// </summary>
        public Task<HttpClient> CreateAuthorizedClientAsync() => AuthorizeAsync(CreateClient());

        /// <summary>
        /// Получить клиент с JWT администратора (логин из Administration:AdminLogins).
        /// </summary>
        public async Task<HttpClient> CreateAdminClientAsync()
        {
            var client = CreateClient();
            // Повторная регистрация вернёт 422 — это нормально, аккаунт уже есть.
            await client.PostAsJsonAsync("Account/register", new CreateAccountDto { Login = AdminLogin, Password = TestPassword });
            return await LoginAsync(client, AdminLogin);
        }

        /// <summary>
        /// Зарегистрировать нового пользователя и добавить его JWT в заголовки клиента.
        /// </summary>
        public static async Task<HttpClient> AuthorizeAsync(HttpClient client)
        {
            var login = $"user_{Guid.NewGuid():N}".Substring(0, 20);

            var registerResponse = await client.PostAsJsonAsync("Account/register", new CreateAccountDto { Login = login, Password = TestPassword });
            registerResponse.EnsureSuccessStatusCode();

            return await LoginAsync(client, login);
        }

        private static async Task<HttpClient> LoginAsync(HttpClient client, string login)
        {
            var loginResponse = await client.PostAsJsonAsync("Account/login", new LoginAccountDto { Login = login, Password = TestPassword });
            loginResponse.EnsureSuccessStatusCode();
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResultDto>();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Token);
            return client;
        }

        /// <summary>
        /// Создать контекст тестовой БД.
        /// </summary>
        /// <returns></returns>
        public BoardDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<BoardDbContext>();
            optionsBuilder.UseInMemoryDatabase(_databaseName);
            optionsBuilder.EnableSensitiveDataLogging();
            var dbContext = new BoardDbContext(optionsBuilder.Options);
            return dbContext;
        }
    }
}