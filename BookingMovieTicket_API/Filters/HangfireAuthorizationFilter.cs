using Hangfire.Dashboard;

namespace BookingMovieTicket_API.Filters;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // Trong môi trường Development cho phép truy cập Dashboard trực tiếp để quan sát background jobs
        // Khi lên Production có thể tích hợp kiểm tra Role Staff hoặc Admin
        return true;
    }
}

