namespace Board.Application.AppData.Common;

/// <summary>
/// Доступ к текущему аутентифицированному пользователю.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// Идентификатор аккаунта текущего пользователя или null, если запрос анонимный.
    /// </summary>
    Guid? GetCurrentAccountId();
}
