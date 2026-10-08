namespace Board.Contracts.Interfaces
{
    /// <summary>
    /// Сервис для работы с запрещёнными словами.
    /// </summary>
    public interface IForbiddenWordsService
    {
        /// <summary>
        /// Получить основы запрещённых слов в нижнем регистре (например, «реклам» для «реклама», «рекламой»).
        /// Слово считается запрещённым, если начинается с основы и после неё идёт окончание
        /// не длиннее <see cref="Attributes.ForbiddenWordsValidationAttribute.MaxEndingLength"/> букв.
        /// </summary>
        /// <returns>Основы запрещённых слов.</returns>
        string[] GetForbiddenWordStems();
    }
}
