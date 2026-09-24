using System;
using System.Collections.Generic;
using System.Text;

namespace BookingMovieTicket.Contracts.Enums
{
    public enum BookingStatus : short
    {
        Pending = 0,
        PaymentProcessing = 1,
        Confirmed = 2,
        CheckedIn = 3,
        Cancelled = 4,
        Expired = 5,
        Refunded = 6
    }
}
