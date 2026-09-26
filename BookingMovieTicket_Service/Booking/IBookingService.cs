using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Booking.Request;
using BookingMovieTicket.Contracts.DTOs.Booking.Response;
using System;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Booking;

public interface IBookingService
{
    /// <summary>
    /// Giữ ghế tạm thời (5 phút) với cơ chế Redis Distributed Lock chống tranh chấp đồng thời
    /// </summary>
    Task<ApiResponse<HoldSeatsResponse>> HoldSeatsAsync(Guid userId, HoldSeatsRequest request);

    /// <summary>
    /// Người dùng chủ động nhả ghế đã giữ
    /// </summary>
    Task<ApiResponse<string>> ReleaseSeatsAsync(Guid userId, ReleaseSeatsRequest request);
}

