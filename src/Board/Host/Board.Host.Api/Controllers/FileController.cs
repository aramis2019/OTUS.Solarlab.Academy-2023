using Board.Application.AppData.Contexts.Files.Services;
using Board.Contracts;
using Board.Host.Api.Extensions;
using Board.Contracts.File;
using Board.Host.Api.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Board.Host.Api.Controllers;

/// <summary>
/// Контроллер для работы с файлами.
/// </summary>
/// <response code="500">Произошла внутренняя ошибка.</response>
[ApiController]
[Route("[controller]")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
public class FileController : ControllerBase
{
    private readonly ILogger<FileController> _logger;
    private readonly IFileService _fileService;
    private readonly FileUploadOptions _uploadOptions;

    /// <summary>
    /// Инициализирует экземпляр <see cref="FileController"/>
    /// </summary>
    /// <param name="fileService">Сервис работы с файлами.</param>
    /// <param name="logger">Сервис логирования.</param>
    /// <param name="uploadOptions">Ограничения на загрузку файлов.</param>
    public FileController(IFileService fileService, ILogger<FileController> logger, IOptions<FileUploadOptions> uploadOptions)
    {
        _logger = logger;
        _fileService = fileService;
        _uploadOptions = uploadOptions.Value;
    }

    /// <summary>
    /// Получение информации о файле по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор файла.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <response code="200">Запрос выполнен успешно.</response>
    /// <response code="404">Файл с указанным идентификатором не найден.</response>
    /// <returns>Информация о файле.</returns>
    [HttpGet("{id:Guid}/info")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInfoById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _fileService.GetInfoByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Загрузка файла в систему.
    /// </summary>
    /// <param name="file">Файл.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <response code="201">Файл успешно загружен.</response>
    /// <response code="400">Файл не передан или пустой.</response>
    /// <response code="413">Файл превышает допустимый размер.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            ModelState.AddModelError(nameof(file), "Файл пустой.");
            return this.InvalidModelState();
        }

        if (file.Length > _uploadOptions.MaxFileSizeBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new ErrorDto
            {
                ErrorCode = "file_too_large",
                UserMessage = $"Размер файла превышает допустимые {_uploadOptions.MaxFileSizeBytes / 1024} КБ."
            });
        }

        var bytes = await GetBytesAsync(file, cancellationToken);
        var fileDto = new FileDto
        {
            Content = bytes,
            ContentType = file.ContentType,
            Name = file.FileName
        };
        var result = await _fileService.UploadAsync(fileDto, cancellationToken);
        return CreatedAtAction(nameof(GetInfoById), new { id = result }, result);
    }

    /// <summary>
    /// Скачивание файла по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор файла.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <response code="200">Запрос выполнен успешно.</response>
    /// <response code="404">Файл с указанным идентификатором не найден.</response>
    /// <returns>Файл в виде потока.</returns>
    [HttpGet("{id:Guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var result = await _fileService.DownloadAsync(id, cancellationToken);

        Response.ContentLength = result.Content.Length;
        return File(result.Content, result.ContentType, result.Name, true);
    }


    /// <summary>
    /// Удаление файла по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор файла.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <response code="403">Доступ запрещён.</response>
    /// <response code="404">Файл с указанным идентификатором не найден.</response>
    [HttpDelete("{id:Guid}")]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _fileService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static async Task<byte[]> GetBytesAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }
}