using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Board.Contracts;
using Board.Contracts.File;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Board.Api.Tests
{
    public class FileTests : IClassFixture<BoardWebApplicationFactory>
    {
        private const int MaxFileSizeBytes = 1024;
        private readonly BoardWebApplicationFactory _webApplicationFactory;

        public FileTests(BoardWebApplicationFactory webApplicationFactory)
        {
            _webApplicationFactory = webApplicationFactory;
        }

        [Fact]
        public async Task File_UploadDownloadDelete_Succeeds()
        {
            var client = await _webApplicationFactory.CreateAuthorizedClientAsync();
            var content = new byte[] { 1, 2, 3, 4, 5 };

            var uploadResponse = await client.PostAsync("File", FileForm(content));

            Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
            var id = await uploadResponse.Content.ReadFromJsonAsync<Guid>();
            Assert.Equal($"/File/{id}/info", uploadResponse.Headers.Location!.AbsolutePath);
            var info = await client.GetFromJsonAsync<FileInfoDto>($"File/{id}/info");
            Assert.Equal(content.Length, info!.Length);
            Assert.Equal(content, await client.GetByteArrayAsync($"File/{id}"));

            var stranger = await _webApplicationFactory.CreateAuthorizedClientAsync();
            Assert.Equal(HttpStatusCode.Forbidden, (await stranger.DeleteAsync($"File/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"File/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"File/{id}")).StatusCode);
        }

        [Fact]
        public async Task File_Upload_Anonymous_Returns401()
        {
            var response = await _webApplicationFactory.CreateClient().PostAsync("File", FileForm(new byte[] { 1 }));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task File_Upload_Empty_Returns400()
        {
            var client = await _webApplicationFactory.CreateAuthorizedClientAsync();

            var response = await client.PostAsync("File", FileForm(Array.Empty<byte>()));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task File_Upload_OverLimit_Returns413()
        {
            var client = await CreateClientWithSmallLimitAsync();

            var response = await client.PostAsync("File", FileForm(new byte[MaxFileSizeBytes + 1]));

            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
            Assert.Equal("file_too_large", (await response.Content.ReadFromJsonAsync<ErrorDto>())!.ErrorCode);
        }

        [Fact]
        public async Task File_Upload_FarOverMultipartLimit_IsRejected()
        {
            var client = await CreateClientWithSmallLimitAsync();

            var response = await client.PostAsync("File", FileForm(new byte[MaxFileSizeBytes + 256 * 1024]));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private async Task<HttpClient> CreateClientWithSmallLimitAsync()
        {
            var factory = _webApplicationFactory.WithWebHostBuilder(builder =>
                builder.UseSetting("FileUpload:MaxFileSizeBytes", MaxFileSizeBytes.ToString()));
            return await BoardWebApplicationFactory.AuthorizeAsync(factory.CreateClient());
        }

        private static MultipartFormDataContent FileForm(byte[] content)
        {
            var fileContent = new ByteArrayContent(content);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return new MultipartFormDataContent { { fileContent, "file", "test.bin" } };
        }
    }
}
