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

    /// <summary>
    /// Является ли текущий пользователь администратором.
    /// </summary>
    bool IsAdmin();

    /// <summary>
    /// Может ли текущий пользователь изменять ресурс с указанным владельцем: владелец или администратор.
    /// </summary>
    bool CanModify(Guid? ownerAccountId) =>
        IsAdmin() || (ownerAccountId != null && ownerAccountId == GetCurrentAccountId());
}
