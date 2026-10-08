using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Board.Contracts.Account;
using Xunit;

namespace Board.Api.Tests
{
    public class AccountTests : IClassFixture<BoardWebApplicationFactory>
    {
        private readonly BoardWebApplicationFactory _webApplicationFactory;

        public AccountTests(BoardWebApplicationFactory webApplicationFactory)
        {
            _webApplicationFactory = webApplicationFactory;
        }

        [Fact]
        public async Task Account_Register_StoresPasswordHash()
        {
            var httpClient = _webApplicationFactory.CreateClient();
            var dto = new CreateAccountDto { Login = $"u_{Guid.NewGuid():N}".Substring(0, 20), Password = "P@ssw0rd!" };

            var response = await httpClient.PostAsJsonAsync("Account/register", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var id = await response.Content.ReadFromJsonAsync<Guid>();
            await using var dbContext = _webApplicationFactory.CreateDbContext();
            var account = await dbContext.FindAsync<Board.Domain.Account.Account>(id);
            Assert.NotNull(account);
            Assert.NotEqual(dto.Password, account!.PasswordHash);
            Assert.DoesNotContain(dto.Password, account.PasswordHash);
        }

        [Fact]
        public async Task Account_GetCurrent_ReturnsCurrentUser()
        {
            var httpClient = await _webApplicationFactory.CreateAuthorizedClientAsync();

            var response = await httpClient.GetAsync("Account/current");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var account = await response.Content.ReadFromJsonAsync<AccountDto>();
            Assert.NotNull(account);
            Assert.StartsWith("user_", account!.Login);
        }

        [Fact]
        public async Task Account_GetCurrent_Anonymous_Unauthorized()
        {
            var response = await _webApplicationFactory.CreateClient().GetAsync("Account/current");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
