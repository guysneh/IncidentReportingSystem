using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace IncidentReportingSystem.API.Filters;

public sealed class LoopbackOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var sp = context.HttpContext.RequestServices;
        var cfg = (IConfiguration)sp.GetService(typeof(IConfiguration))!;
        var env = (IHostEnvironment)sp.GetService(typeof(IHostEnvironment))!;
        var mode = cfg["Attachments:Storage"];

        var isLoopback = string.Equals(mode, "Loopback", StringComparison.OrdinalIgnoreCase)
                         || (string.IsNullOrWhiteSpace(mode) && env.IsDevelopment());

        if (!isLoopback)
            context.Result = new NotFoundResult(); 
    }
}
