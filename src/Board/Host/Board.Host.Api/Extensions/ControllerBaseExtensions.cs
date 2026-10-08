using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Board.Host.Api.Extensions;

/// <summary>
/// Расширения для контроллеров.
/// </summary>
public static class ControllerBaseExtensions
{
    /// <summary>
    /// Ответ 400 с ошибками из ModelState в том же формате, что и при автоматической валидации модели.
    /// </summary>
    public static IActionResult InvalidModelState(this ControllerBase controller)
    {
        var options = controller.HttpContext.RequestServices.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
        return options.InvalidModelStateResponseFactory(controller.ControllerContext);
    }
}
