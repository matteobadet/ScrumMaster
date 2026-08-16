using System.Net;
using ScrumMaster.Api.Services;

namespace ScrumMaster.Api.Middleware;

/// <summary>Traduit les exceptions métier (Services/DomainExceptions.cs) en réponses HTTP.</summary>
public class DomainExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainValidationException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (DomainNotFoundException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (DomainForbiddenException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.Forbidden, ex.Message);
        }
    }

    private static Task WriteProblemAsync(HttpContext context, HttpStatusCode status, string message)
    {
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new { message });
    }
}
