using Hangfire.Dashboard;

namespace Enterprise.WebApi.Common;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In development, allow all
        // In production, check for admin role or use proper authentication
        return true;
    }
}
