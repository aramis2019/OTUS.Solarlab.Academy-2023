using Board.Application.AppData.Contexts.Adverts.Services;
using Board.Contracts;
using Board.Contracts.Advert;
using Board.Host.Api.Extensions;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Board.Host.Api.Controllers;

/// <summary>
/// Контроллер для работы с объявлениями.
/// </summary>
/// <response code="500">Произошла внутренняя ошибка.</response>
[ApiController]
[Route("[controller]")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
public class AdvertController : ControllerBase
{
    private readonly ILogger<AdvertController> _logger;
    private readonly IAdvertService _advertService;

    /// <summary>
    /// Инициализирует экземпляр <see cref="AdvertController"/>
    /// </summary>
    /// <param name="logger">Сервис логирования.</param>
    /// <param name="advertService">Сервис для работы с объявлениями.</param>
    public AdvertController(ILogger<AdvertController> logger, IAdvertService advertService)
    {
        _logger = logger;
        _advertService = advertService;
    }

    /// <summary>
    /// Получить страницу активных объявлений, от новых к старым.
    /// </summary>
    /// <param name="page">Параметры страницы: skip (по умолчанию 0) и take (по умолчанию 20, максимум 100).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <response code="200">Запрос выполнен успешно</response>
    /// <response code="400">Некорректные параметры страницы.</response>
    /// <returns>Список моделей объявлений.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AdvertShortInfoDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] PageRequestDto page, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос списка объявлений: skip={Skip}, take={Take}", page.Skip, page.Take);
        var result = await _advertService.GetAll(page, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Получить объявление по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <response code="200">Запрос выполнен успешно.</response>
    /// <response code="404">Объявление с указанным идентификатором не найдено.</response>
    /// <returns>Модель объявления.</returns>
    [HttpGet("{id:Guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AdvertInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Запрос объявления по идентификатору: {id}");
        var result = await _advertService.Get(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Создать новое объявление.
    /// </summary>
    /// <param name="dto">Модель создания объявления.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <response code="201">Объявление успешно создано.</response>
    /// <response code="400">Модель данных запроса невалидна.</response>
    /// <response code="422">Произошёл конфликт бизнес-логики.</response>
    /// <returns>Модель созданного объявления.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AdvertInfoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateAdvertDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на создание объявления {Name}", dto.Name);
        var result = await _advertService.Add(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Обновить объявление.
    /// </summary>
    /// <param name="id">Идентификатор.</param>
    /// <param name="dto">Модель обновления объявления.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <response code="200">Запрос выполнен успешно.</response>
    /// <response code="400">Модель данных запроса невалидна.</response>
    /// <response code="403">Доступ запрещён.</response>
    /// <response code="404">Объявление с указанным идентификатором не найдено.</response>
    /// <response code="422">Произошёл конфликт бизнес-логики.</response>
    /// <returns>Модель обновлённого объявления.</returns>
    [HttpPut("{id:Guid}")]
    [ProducesResponseType(typeof(AdvertInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdvertDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на обновление объявления {Id}", id);
        var result = await _advertService.Update(id, dto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Частично обновить объявление.
    /// </summary>
    /// <param name="id">Идентификатор.</param>
    /// <param name="dto">Модель обновления объявления.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <response code="200">Запрос выполнен успешно.</response>
    /// <response code="400">Модель данных запроса невалидна.</response>
    /// <response code="403">Доступ запрещён.</response>
    /// <response code="404">Объявление с указанным идентификатором не найдено.</response>
    /// <response code="422">Произошёл конфликт бизнес-логики.</response>
    /// <returns>Модель обновлённого объявления.</returns>
    [HttpPatch("{id:Guid}")]
    [ProducesResponseType(typeof(AdvertInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Patch(Guid id, [FromBody] JsonPatchDocument<UpdateAdvertDto> dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на частичное обновление объявления {Id}", id);
        var model = await _advertService.GetForUpdate(id, cancellationToken);

        dto.ApplyTo(model, error => ModelState.AddModelError(error.Operation.path ?? string.Empty, error.ErrorMessage));
        if (!ModelState.IsValid || !TryValidateModel(model))
        {
            return this.InvalidModelState();
        }

        var result = await _advertService.Update(id, model, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Удалить объявление по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <response code="204">Запрос выполнен успешно.</response>
    /// <response code="403">Доступ запрещён.</response>
    [HttpDelete("{id:Guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Запрос на удаление объявления по идентификатору: {id}");
        await _advertService.Delete(id, cancellationToken);
        return NoContent();
    }
}