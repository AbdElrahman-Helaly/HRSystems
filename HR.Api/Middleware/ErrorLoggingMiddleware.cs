using internalEmployee.Data;
using internalEmployee.Data.Entities;
using System.Security.Claims;

namespace internalEmployee.Middleware;

public class ErrorLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            Guid? userId = null;
            var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? context.User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedId))
                userId = parsedId;

            var log = new SystemErrorLog
            {
                UserId = userId,
                Path = context.Request?.Path.Value,
                Method = context.Request?.Method,
                QueryString = context.Request?.QueryString.Value,
                StatusCode = context.Response?.StatusCode,
                Message = ex.Message,
                ExceptionType = ex.GetType().FullName,
                StackTrace = ex.StackTrace,
                TraceId = context.TraceIdentifier,
                RemoteIp = context.Connection?.RemoteIpAddress?.ToString()
            };

            dbContext.SystemErrorLogs.Add(log);
            await dbContext.SaveChangesAsync();

            context.Items["ErrorLogged"] = true;

            throw;
        }
    }
}

public static class ErrorLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorDbLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorLoggingMiddleware>();
    }
}
