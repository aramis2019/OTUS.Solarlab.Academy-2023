using Board.Contracts.Interfaces;

namespace Board.Application.AppData.Services
{
    /// <inheritdoc />
    public class ForbiddenWordsService : IForbiddenWordsService
    {
        /// <inheritdoc />
        public string[] GetForbiddenWordStems()
        {
            return new[] { "дурак", "реклам", "взятк" };
        }
    }
}
