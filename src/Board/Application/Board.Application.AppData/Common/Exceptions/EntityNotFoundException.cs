namespace Board.Application.AppData.Common.Exceptions;

/// <summary>
/// Запрошенная сущность не найдена.
/// </summary>
public class EntityNotFoundException : BusinessException
{
    public EntityNotFoundException(string message) : base(message)
    {
    }
}
