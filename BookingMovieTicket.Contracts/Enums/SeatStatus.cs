namespace BookingMovieTicket.Contracts.Enums
{
    /// <summary>
    /// Định nghĩa các vai trò trong hệ thống
    /// 0: Available — còn trống
    /// 1: Held — đang giữ tạm
    /// 2: Booked — đã bán
    /// 3: Disabled — không bán
    /// </summary>
    public enum SeatStatus : short
    {
        Available = 0,
        Held = 1,
        Booked = 2,
        Disabled = 3,
    }
}
