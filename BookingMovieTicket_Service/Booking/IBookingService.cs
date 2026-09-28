using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Booking.Request;
using BookingMovieTicket.Contracts.DTOs.Booking.Response;
using PayOS.Models.Webhooks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Booking;

public interface IBookingService
{
    /// <summary>
    /// Giữ ghế tạm thời (5 phút)
    /// </summary>
    Task<ApiResponse<HoldSeatsResponse>> HoldSeatsAsync(Guid userId, HoldSeatsRequest request);

    /// <summary>
    /// Người dùng chủ động nhả ghế đã giữ
    /// </summary>
    Task<ApiResponse<string>> ReleaseSeatsAsync(Guid userId, ReleaseSeatsRequest request);

    /// <summary>
    /// Tạo đơn đặt vé từ các ghế đang giữ và sinh link thanh toán PayOS VietQR
    /// </summary>
    Task<ApiResponse<CheckoutResponse>> CheckoutAsync(Guid userId, CheckoutRequest request);

    /// <summary>
    /// Xử lý Webhook từ PayOS khi khách hàng chuyển khoản thành công
    /// </summary>
    Task<ApiResponse<string>> HandlePayOSWebhookAsync(Webhook webhookBody);

    /// <summary>
    /// Chủ động đồng bộ trạng thái thanh toán từ PayOS
    /// </summary>
    Task<ApiResponse<BookingDetailResponse>> SyncPayOSPaymentAsync(Guid bookingId);

    /// <summary>
    /// Khách hàng xem danh sách lịch sử các đơn đặt vé của chính mình
    /// </summary>
    Task<ApiResponse<List<BookingHistoryResponse>>> GetMyBookingsAsync(Guid userId);

    /// <summary>
    /// Xem thông tin chi tiết một vé đã đặt
    /// </summary>
    Task<ApiResponse<BookingDetailResponse>> GetBookingByIdAsync(Guid userId, Guid bookingId, bool isStaff = false);

    /// <summary>
    /// Nhân viên soát vé / check-in vé tại rạp bằng mã QR hoặc TicketCode
    /// </summary>
    Task<ApiResponse<BookingDetailResponse>> CheckInTicketAsync(CheckInTicketRequest request);
}

