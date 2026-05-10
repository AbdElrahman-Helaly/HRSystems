using internalEmployee.Data;
using internalEmployee.Data.Entities;
using System.Security.Claims;

namespace internalEmployee.Middleware;

public class StatusCodeLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public StatusCodeLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        await _next(context);

        if (context.Items.ContainsKey("ErrorLogged"))
            return;

        var statusCode = context.Response?.StatusCode ?? 200;
        if (statusCode < 400)
            return;

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
            StatusCode = statusCode,
            Message = $"HTTP {statusCode}",
            ExceptionType = null,
            StackTrace = null,
            TraceId = context.TraceIdentifier,
            RemoteIp = context.Connection?.RemoteIpAddress?.ToString()
        };

        dbContext.SystemErrorLogs.Add(log);
        await dbContext.SaveChangesAsync();
    }
}

public static class StatusCodeLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseStatusCodeDbLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<StatusCodeLoggingMiddleware>();
    }
}
