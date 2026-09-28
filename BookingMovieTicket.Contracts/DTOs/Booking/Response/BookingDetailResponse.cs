using System;
using System.Collections.Generic;

namespace BookingMovieTicket.Contracts.DTOs.Booking.Response;

public class BookingDetailResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public Guid ShowtimeId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string? VenueAddress { get; set; }
    public string SeatMapName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal TotalAmount { get; set; }
    public short Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? QrCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public List<BookingSeatItemDto> Seats { get; set; } = new();
    public BookingPaymentDto? Payment { get; set; }
}

public class BookingSeatItemDto
{
    public Guid SeatId { get; set; }
    public string RowLabel { get; set; } = string.Empty;
    public int Number { get; set; }
    public string SeatCode => $"{RowLabel}{Number}";
    public decimal Price { get; set; }
    public short SeatType { get; set; }
    public string SeatTypeName { get; set; } = string.Empty;
}

public class BookingPaymentDto
{
    public decimal Amount { get; set; }
    public short Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public short Method { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public string? TransactionRef { get; set; }
    public DateTime CreatedAt { get; set; }
}

