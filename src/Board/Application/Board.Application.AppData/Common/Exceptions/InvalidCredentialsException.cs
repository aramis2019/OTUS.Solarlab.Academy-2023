namespace Board.Application.AppData.Common.Exceptions;

/// <summary>
/// Неверный логин или пароль.
/// </summary>
public class InvalidCredentialsException : BusinessException
{
    public InvalidCredentialsException() : base("Неверный логин или пароль.")
    {
    }
}
