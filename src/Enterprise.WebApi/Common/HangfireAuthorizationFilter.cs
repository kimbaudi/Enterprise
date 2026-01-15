using Hangfire.Dashboard;

namespace Enterprise.WebApi.Common;

/// <summary>
/// Authorization filter for Hangfire Dashboard that requires Admin role.
/// Only authenticated users with Admin role can access the dashboard.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Require authentication
        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // Require Admin role
        return httpContext.User.IsInRole("Admin");
    }
}
