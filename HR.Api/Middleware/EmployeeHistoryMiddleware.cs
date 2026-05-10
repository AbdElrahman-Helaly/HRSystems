using internalEmployee.Data;
using System.Security.Claims;

namespace internalEmployee.Middleware;

public class EmployeeHistoryMiddleware
{
    private readonly RequestDelegate _next;

    public EmployeeHistoryMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        // Extract user ID from JWT token (check both standard and JWT specific claims)
        var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? context.User?.FindFirst("sub")?.Value;
        
        if (!string.IsNullOrWhiteSpace(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
        {
            // Set current user in DbContext for automatic tracking
            dbContext.SetCurrentUser(userId);
        }

        await _next(context);
    }
}

// Extension method to register middleware
public static class EmployeeHistoryMiddlewareExtensions
{
    public static IApplicationBuilder UseEmployeeHistoryTracking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<EmployeeHistoryMiddleware>();
    }
}
