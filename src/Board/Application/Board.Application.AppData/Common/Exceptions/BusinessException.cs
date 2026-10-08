namespace Board.Application.AppData.Common.Exceptions;

/// <summary>
/// Базовое исключение прикладного уровня. Сообщение показывается пользователю.
/// </summary>
public abstract class BusinessException : Exception
{
    protected BusinessException(string message) : base(message)
    {
    }
}
