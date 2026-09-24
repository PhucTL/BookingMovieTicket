namespace BookingMovieTicket.Contracts.Enums
{
    /// <summary>
    /// Định nghĩa các vai trò trong hệ thống
    /// 0: Standard — ghế thường
    /// 1: Vip — ghế VIP
    /// 2: Couple — ghế đôi
    /// </summary>
    public enum SeatType : short
    {
        Standard = 0,
        Vip = 1,
        Couple = 2
    }
}
