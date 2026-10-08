namespace Board.Application.AppData.Contexts.Accounts.Services;

/// <summary>
/// Хеширование и проверка паролей.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Получить хеш пароля (включает соль и параметры алгоритма).
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Проверить, соответствует ли пароль сохранённому хешу.
    /// </summary>
    bool Verify(string password, string passwordHash);
}
