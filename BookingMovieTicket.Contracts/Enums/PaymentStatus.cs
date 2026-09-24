using System;
using System.Collections.Generic;
using System.Text;

namespace BookingMovieTicket.Contracts.Enums
{
    public enum PaymentStatus : short
    {
        Pending = 0,
        Success = 1,
        Failed = 2,
        Refunded = 3
    }
}
