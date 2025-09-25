using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Classroom_Dashboard_Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ErrorController : ControllerBase
    {
        [HttpGet]
        [HttpPost]
        [HttpPut]
        [HttpDelete]
        public IActionResult HandleError()
        {
            var exceptionHandlerFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var exception = exceptionHandlerFeature?.Error;

            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<ErrorController>>();
            logger.LogError(exception, "An unhandled exception occurred");

            return Problem(
                title: "An error occurred while processing your request",
                statusCode: 500
            );
        }
    }
}
