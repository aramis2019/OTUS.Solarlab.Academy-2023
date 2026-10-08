namespace Board.Application.AppData.Common.Exceptions;

/// <summary>
/// У текущего пользователя нет прав на операцию.
/// </summary>
public class AccessDeniedException : BusinessException
{
    public AccessDeniedException(string message) : base(message)
    {
    }
}
