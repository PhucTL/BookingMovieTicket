using System.Threading.Tasks;

namespace BookingMovieTicket_Service.BackgroundJobs;

public interface ISeatExpirationJob
{
    /// <summary>
    /// Tiến trình nền quét và giải phóng các ghế giữ tạm (Held) đã quá 5 phút mà chưa hoàn tất thanh toán
    /// </summary>
    Task ReleaseExpiredSeatsAsync();
}

