namespace Board.Application.AppData.Common;

/// <summary>
/// Роли пользователей.
/// </summary>
public static class Roles
{
    /// <summary>
    /// Администратор: управляет категориями, может изменять и удалять любые объявления и файлы.
    /// Логины администраторов задаются в конфигурации (Administration:AdminLogins).
    /// </summary>
    public const string Admin = "Admin";
}
