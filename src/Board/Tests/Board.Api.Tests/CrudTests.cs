using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Board.Contracts;
using Board.Contracts.Advert;
using Board.Contracts.Category;
using Xunit;

namespace Board.Api.Tests
{
    public class CrudTests : IClassFixture<BoardWebApplicationFactory>
    {
        private readonly BoardWebApplicationFactory _webApplicationFactory;

        public CrudTests(BoardWebApplicationFactory webApplicationFactory)
        {
            _webApplicationFactory = webApplicationFactory;
        }

        [Theory]
        [InlineData("Advert/{0}")]
        [InlineData("Category/{0}")]
        [InlineData("File/{0}")]
        [InlineData("File/{0}/info")]
        public async Task GetById_Unknown_Returns404(string urlTemplate)
        {
            var response = await _webApplicationFactory.CreateClient().GetAsync(string.Format(urlTemplate, Guid.NewGuid()));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("not_found", (await response.Content.ReadFromJsonAsync<ErrorDto>())!.ErrorCode);
        }

        [Fact]
        public async Task Advert_Create_ReturnsLocationOfCreatedAdvert()
        {
            var client = await _webApplicationFactory.CreateAuthorizedClientAsync();

            var response = await client.PostAsJsonAsync("Advert", NewAdvert(DataSeedHelper.TestCategoryId));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<AdvertInfoDto>();
            Assert.Equal($"/Advert/{created!.Id}", response.Headers.Location!.AbsolutePath);
            var fetched = await client.GetFromJsonAsync<AdvertInfoDto>(response.Headers.Location);
            Assert.Equal(created.Name, fetched!.Name);
        }

        [Fact]
        public async Task Advert_Create_UnknownCategory_Returns422()
        {
            var client = await _webApplicationFactory.CreateAuthorizedClientAsync();

            var response = await client.PostAsJsonAsync("Advert", NewAdvert(Guid.NewGuid()));

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        }

        [Fact]
        public async Task Advert_UpdateAndPatch_ByAuthor_Succeeds()
        {
            var client = await _webApplicationFactory.CreateAuthorizedClientAsync();
            var created = await (await client.PostAsJsonAsync("Advert", NewAdvert(DataSeedHelper.TestCategoryId)))
                .Content.ReadFromJsonAsync<AdvertInfoDto>();

            var update = NewAdvert(DataSeedHelper.TestCategoryId);
            update.Name = "updated_name";
            update.Price = 100;
            var putResponse = await client.PutAsJsonAsync($"Advert/{created!.Id}", update);

            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
            var updated = await putResponse.Content.ReadFromJsonAsync<AdvertInfoDto>();
            Assert.Equal("updated_name", updated!.Name);
            Assert.Equal(100, updated.Price);
            Assert.Equal(created.CreatedAt, updated.CreatedAt);
            Assert.True(updated.IsActive);

            var patchResponse = await client.PatchAsync($"Advert/{created.Id}", JsonPatch("/name", "patched_name"));

            Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
            var fetched = await client.GetFromJsonAsync<AdvertInfoDto>($"Advert/{created.Id}");
            Assert.Equal("patched_name", fetched!.Name);
            Assert.Equal(100, fetched.Price);
        }

        [Fact]
        public async Task Advert_Patch_InvalidValue_Returns400()
        {
            var client = await _webApplicationFactory.CreateAuthorizedClientAsync();
            var created = await (await client.PostAsJsonAsync("Advert", NewAdvert(DataSeedHelper.TestCategoryId)))
                .Content.ReadFromJsonAsync<AdvertInfoDto>();

            var response = await client.PatchAsync($"Advert/{created!.Id}", JsonPatch("/name", "x"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
            Assert.Contains(error!.InternalErrors, e => e.ErrorCode == "Name");
        }

        [Fact]
        public async Task Advert_Update_ByNotAuthor_Returns403()
        {
            var author = await _webApplicationFactory.CreateAuthorizedClientAsync();
            var stranger = await _webApplicationFactory.CreateAuthorizedClientAsync();
            var created = await (await author.PostAsJsonAsync("Advert", NewAdvert(DataSeedHelper.TestCategoryId)))
                .Content.ReadFromJsonAsync<AdvertInfoDto>();

            var putResponse = await stranger.PutAsJsonAsync($"Advert/{created!.Id}", NewAdvert(DataSeedHelper.TestCategoryId));
            var patchResponse = await stranger.PatchAsync($"Advert/{created.Id}", JsonPatch("/name", "hacked_name"));

            Assert.Equal(HttpStatusCode.Forbidden, putResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, patchResponse.StatusCode);
        }

        [Fact]
        public async Task Category_Crud_Succeeds()
        {
            var client = await _webApplicationFactory.CreateAuthorizedClientAsync();

            var createResponse = await client.PostAsJsonAsync("Category", new CreateCategoryDto { Name = "Parent" });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var parentId = await createResponse.Content.ReadFromJsonAsync<Guid>();
            Assert.Equal($"/Category/{parentId}", createResponse.Headers.Location!.AbsolutePath);

            var childId = await (await client.PostAsJsonAsync("Category", new CreateCategoryDto { Name = "Child", ParentId = parentId }))
                .Content.ReadFromJsonAsync<Guid>();

            var putResponse = await client.PutAsJsonAsync($"Category/{childId}", new UpdateCategoryDto { Name = "Renamed", ParentId = parentId });
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
            Assert.Equal("Renamed", (await putResponse.Content.ReadFromJsonAsync<CategoryInfoDto>())!.Name);

            var patchResponse = await client.PatchAsync($"Category/{childId}", JsonPatch("/isActive", false));
            Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
            var patched = await client.GetFromJsonAsync<CategoryInfoDto>($"Category/{childId}");
            Assert.False(patched!.IsActive);
            Assert.Equal("Renamed", patched.Name);

            // Родителя с дочерней категорией удалить нельзя.
            Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.DeleteAsync($"Category/{parentId}")).StatusCode);

            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"Category/{childId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"Category/{parentId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"Category/{parentId}")).StatusCode);
        }

        [Fact]
        public async Task Category_Delete_WithAdverts_Returns422()
        {
            var client = await _webApplicationFactory.CreateAuthorizedClientAsync();

            var response = await client.DeleteAsync($"Category/{DataSeedHelper.TestCategoryId}");

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        }

        [Fact]
        public async Task Category_Update_Cycle_Returns422()
        {
            var client = await _webApplicationFactory.CreateAuthorizedClientAsync();
            var aId = await (await client.PostAsJsonAsync("Category", new CreateCategoryDto { Name = "Cat A" })).Content.ReadFromJsonAsync<Guid>();
            var bId = await (await client.PostAsJsonAsync("Category", new CreateCategoryDto { Name = "Cat B", ParentId = aId })).Content.ReadFromJsonAsync<Guid>();

            var selfResponse = await client.PutAsJsonAsync($"Category/{aId}", new UpdateCategoryDto { Name = "Cat A", ParentId = aId });
            var cycleResponse = await client.PutAsJsonAsync($"Category/{aId}", new UpdateCategoryDto { Name = "Cat A", ParentId = bId });
            var unknownParentResponse = await client.PostAsJsonAsync("Category", new CreateCategoryDto { Name = "Cat C", ParentId = Guid.NewGuid() });

            Assert.Equal(HttpStatusCode.UnprocessableEntity, selfResponse.StatusCode);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, cycleResponse.StatusCode);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownParentResponse.StatusCode);
        }

        [Fact]
        public async Task Category_Create_Anonymous_Returns401()
        {
            var response = await _webApplicationFactory.CreateClient().PostAsJsonAsync("Category", new CreateCategoryDto { Name = "Anon" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Advert_GetAll_IsPagedNewestFirst()
        {
            var client = await _webApplicationFactory.CreateAuthorizedClientAsync();
            for (var i = 0; i < 3; i++)
            {
                var advert = NewAdvert(DataSeedHelper.TestCategoryId);
                advert.Name = $"paged_{i}";
                await client.PostAsJsonAsync("Advert", advert);
            }

            var firstPage = await client.GetFromJsonAsync<AdvertShortInfoDto[]>("Advert?take=2");
            var secondPage = await client.GetFromJsonAsync<AdvertShortInfoDto[]>("Advert?skip=1&take=2");

            Assert.Equal(2, firstPage!.Length);
            Assert.Equal("paged_2", firstPage[0].Name);
            Assert.Equal("paged_1", firstPage[1].Name);
            Assert.Equal(firstPage[1].Id, secondPage![0].Id);
        }

        [Theory]
        [InlineData("Advert?take=0")]
        [InlineData("Advert?take=101")]
        [InlineData("Advert?skip=-1")]
        public async Task Advert_GetAll_InvalidPage_Returns400(string url)
        {
            var response = await _webApplicationFactory.CreateClient().GetAsync(url);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private static UpdateAdvertDto NewAdvert(Guid categoryId) => new()
        {
            Name = "test_name",
            Description = "test_description",
            CategoryId = categoryId,
            Address = "some_city"
        };

        private static StringContent JsonPatch(string path, object value) =>
            new($"[{{\"op\":\"replace\",\"path\":\"{path}\",\"value\":{System.Text.Json.JsonSerializer.Serialize(value)}}}]",
                Encoding.UTF8, "application/json-patch+json");
    }
}
