namespace Board.Application.AppData.Common.Exceptions;

/// <summary>
/// Запрос корректен, но нарушает бизнес-правило (например, логин уже занят).
/// </summary>
public class BusinessRuleException : BusinessException
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
