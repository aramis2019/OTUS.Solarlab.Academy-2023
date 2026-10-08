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

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Jwt:Key", TestJwtKey);
            // Тесты регистрируют много пользователей с одного «адреса»; сам лимит проверяется отдельным тестом.
            builder.UseSetting("RateLimiting:Auth:PermitLimit", "100000");

            builder.ConfigureServices(services =>
            {
                var descriptor =
                    services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextOptionsConfigurator<BoardDbContext>));

                services.Remove(descriptor!);

                services.AddSingleton<IDbContextOptionsConfigurator<BoardDbContext>, TestBoardDbContextConfiguration>();
                
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
        /// Зарегистрировать нового пользователя и добавить его JWT в заголовки клиента.
        /// </summary>
        public static async Task<HttpClient> AuthorizeAsync(HttpClient client)
        {
            var login = $"user_{Guid.NewGuid():N}".Substring(0, 20);
            const string password = "P@ssw0rd!";

            var registerResponse = await client.PostAsJsonAsync("Account/register", new CreateAccountDto { Login = login, Password = password });
            registerResponse.EnsureSuccessStatusCode();

            var loginResponse = await client.PostAsJsonAsync("Account/login", new LoginAccountDto { Login = login, Password = password });
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
            optionsBuilder.UseInMemoryDatabase(TestBoardDbContextConfiguration.InMemoryDatabaseName);
            optionsBuilder.EnableSensitiveDataLogging();
            var dbContext = new BoardDbContext(optionsBuilder.Options);
            return dbContext;
        }
    }
}