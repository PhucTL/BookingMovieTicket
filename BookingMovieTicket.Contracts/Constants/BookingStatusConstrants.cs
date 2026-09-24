using System;
using System.Collections.Generic;
using System.Text;

namespace BookingMovieTicket.Contracts.Constants
{
    public static class BookingStatusConstrants
    {
        public const string Pending = "Pending";
        public const string PaymentProcessing = "PaymentProcessing";
        public const string Confirmed = "Confirmed";
        public const string CheckedIn = "CheckedIn";
        public const string Cancelled = "Cancelled";
        public const string Expired = "Expired";
        public const string Refunded = "Refunded";
    }
}
