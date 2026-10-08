using Board.Application.AppData.Contexts.Accounts.Services;
using Board.Contracts;
using Board.Contracts.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Board.Host.Api.Controllers;

/// <summary>
/// Контроллер для работы с аккаунтами.
/// </summary>
/// <response code="500">Произошла внутренняя ошибка.</response>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(ErrorDto), StatusCodes.Status500InternalServerError)]
public class AccountController : ControllerBase
{
    private readonly ILogger<AccountController> _logger;
    private readonly IAccountService _accountService;

    /// <summary>
    /// Инициализирует экземпляр <see cref="AccountController"/>
    /// </summary>
    /// <param name="logger">Сервис логирования.</param>
    public AccountController(ILogger<AccountController> logger, IAccountService accountService)
    {
        _logger = logger;
        _accountService = accountService;
    }

    /// <summary>
    /// Зарегистрировать новый аккаунт.
    /// </summary>
    /// <param name="dto">Модель регистрации аккаунта.</param>
    /// <param name="cancellation">Токен отмены.</param>
    /// <response code="201">Аккаунт успешно зарегистрирован.</response>
    /// <response code="400">Модель данных запроса невалидна.</response>
    /// <response code="422">Пользователь с таким логином уже зарегистрирован.</response>
    /// <returns>Идентификатор зарегистрированного аккаунта.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterAccount([FromBody] CreateAccountDto dto, CancellationToken cancellation)
    {
        _logger.LogInformation("Регистрация нового аккаунта.");
        var result = await _accountService.RegisterAccountAsync(dto, cancellation);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Войти в аккаунт.
    /// </summary>
    /// <param name="dto">Модель входа в аккаунт.</param>
    /// <param name="cancellation">Токен отмены.</param>
    /// <response code="200">Запрос выполнен успешно</response>
    /// <response code="400">Модель данных запроса невалидна.</response>
    /// <response code="401">Неверный логин или пароль.</response>
    /// <returns>Модель с JWT для заголовка Authorization.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginAccountDto dto, CancellationToken cancellation)
    {
        _logger.LogInformation("Вход в аккаунт.");
        var result = await _accountService.LoginAsync(dto, cancellation);
        return Ok(result);
    }

    /// <summary>
    /// Получить информацию о текущем пользователе.
    /// </summary>
    /// <param name="cancellation">Токен отмены.</param>
    /// <response code="200">Запрос выполнен успешно.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <returns>Модель текущего аккаунта.</returns>
    [HttpGet("current")]
    [Authorize]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellation)
    {
        var result = await _accountService.GetCurrentAsync(cancellation);
        return Ok(result);
    }
}
