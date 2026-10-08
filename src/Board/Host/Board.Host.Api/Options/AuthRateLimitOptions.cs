namespace Board.Host.Api.Options;

/// <summary>
/// Ограничение частоты запросов к входу и регистрации (секция конфигурации <c>RateLimiting:Auth</c>).
/// Лимит считается отдельно для каждого IP-адреса клиента.
/// </summary>
public class AuthRateLimitOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SectionName = "RateLimiting:Auth";

    /// <summary>
    /// Имя политики для атрибута EnableRateLimiting.
    /// </summary>
    public const string PolicyName = "auth";

    /// <summary>
    /// Сколько запросов разрешено за окно.
    /// </summary>
    public int PermitLimit { get; set; } = 10;

    /// <summary>
    /// Длительность окна в секундах.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;
}
