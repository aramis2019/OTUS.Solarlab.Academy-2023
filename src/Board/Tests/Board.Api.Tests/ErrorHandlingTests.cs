using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Board.Contracts;
using Board.Contracts.Account;
using Board.Contracts.Advert;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Board.Api.Tests
{
    public class ErrorHandlingTests : IClassFixture<BoardWebApplicationFactory>
    {
        private readonly BoardWebApplicationFactory _webApplicationFactory;

        public ErrorHandlingTests(BoardWebApplicationFactory webApplicationFactory)
        {
            _webApplicationFactory = webApplicationFactory;
        }

        [Theory]
        [InlineData("no_such_user")]
        [InlineData(null)]
        public async Task Login_InvalidCredentials_Returns401WithSameMessage(string? login)
        {
            var httpClient = _webApplicationFactory.CreateClient();
            if (login == null)
            {
                login = $"u_{Guid.NewGuid():N}".Substring(0, 20);
                await httpClient.PostAsJsonAsync("Account/register", new CreateAccountDto { Login = login, Password = "P@ssw0rd!" });
            }

            var response = await httpClient.PostAsJsonAsync("Account/login", new LoginAccountDto { Login = login, Password = "wrong-password" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
            Assert.Equal("invalid_credentials", error!.ErrorCode);
            Assert.Equal("Неверный логин или пароль.", error.UserMessage);
        }

        [Fact]
        public async Task Register_DuplicateLogin_Returns422()
        {
            var httpClient = _webApplicationFactory.CreateClient();
            var dto = new CreateAccountDto { Login = $"u_{Guid.NewGuid():N}".Substring(0, 20), Password = "P@ssw0rd!" };
            await httpClient.PostAsJsonAsync("Account/register", dto);

            var response = await httpClient.PostAsJsonAsync("Account/register", dto);

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
            Assert.Equal("business_rule_violation", error!.ErrorCode);
        }

        [Fact]
        public async Task InvalidModel_Returns400WithFieldErrors()
        {
            var httpClient = await _webApplicationFactory.CreateAuthorizedClientAsync();

            var response = await httpClient.PostAsJsonAsync("Advert", new CreateAdvertDto { Name = "x" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
            Assert.Equal("validation_error", error!.ErrorCode);
            Assert.Contains(error.InternalErrors!, e => e.ErrorCode == "Name");
            Assert.Contains(error.InternalErrors!, e => e.ErrorCode == "Address");
            Assert.Contains(error.InternalErrors!, e => e.ErrorCode == "Description");
        }

        [Fact]
        public async Task Advert_DeleteByNotAuthor_Returns403()
        {
            var author = await _webApplicationFactory.CreateAuthorizedClientAsync();
            var stranger = await _webApplicationFactory.CreateAuthorizedClientAsync();
            var createResponse = await author.PostAsJsonAsync("Advert", new CreateAdvertDto
            {
                Name = "test_name",
                Description = "test_description",
                CategoryId = DataSeedHelper.TestCategoryId,
                Address = "some_city"
            });
            var id = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

            var strangerResponse = await stranger.DeleteAsync($"Advert/{id}");
            var authorResponse = await author.DeleteAsync($"Advert/{id}");

            Assert.Equal(HttpStatusCode.Forbidden, strangerResponse.StatusCode);
            Assert.Equal("forbidden", (await strangerResponse.Content.ReadFromJsonAsync<ErrorDto>())!.ErrorCode);
            Assert.Equal(HttpStatusCode.NoContent, authorResponse.StatusCode);
        }

        [Fact]
        public async Task Login_TooManyAttempts_Returns429()
        {
            var client = _webApplicationFactory
                .WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:Auth:PermitLimit", "3"))
                .CreateClient();
            var dto = new LoginAccountDto { Login = "no_such_user", Password = "wrong-password" };

            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("Account/login", dto)).StatusCode);
            }
            var response = await client.PostAsJsonAsync("Account/login", dto);

            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.True(response.Headers.Contains("Retry-After"));
            Assert.Equal("too_many_requests", (await response.Content.ReadFromJsonAsync<ErrorDto>())!.ErrorCode);
        }
    }
}
