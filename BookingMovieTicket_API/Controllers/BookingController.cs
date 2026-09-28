using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.Constants;
using BookingMovieTicket.Contracts.DTOs.Booking.Request;
using BookingMovieTicket.Contracts.DTOs.Booking.Response;
using BookingMovieTicket_Service.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookingMovieTicket_API.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Giữ ghế tạm thời trong 5 phút để chuẩn bị thanh toán
    /// </summary>
    [HttpPost("hold-seats")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<HoldSeatsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HoldSeatsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> HoldSeats([FromBody] HoldSeatsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<HoldSeatsResponse>.ErrorResult("Dữ liệu yêu cầu không hợp lệ."));
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<HoldSeatsResponse>.ErrorResult("Không thể xác thực người dùng."));
        }

        var result = await _bookingService.HoldSeatsAsync(userId.Value, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Người dùng nhả ghế đang giữ
    /// </summary>
    [HttpPost("release-seats")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReleaseSeats([FromBody] ReleaseSeatsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.ErrorResult("Dữ liệu yêu cầu không hợp lệ."));
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<string>.ErrorResult("Không thể xác thực người dùng."));
        }

        var result = await _bookingService.ReleaseSeatsAsync(userId.Value, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Tạo đơn đặt vé từ các ghế đang giữ và nhận link thanh toán PayOS VietQR
    /// </summary>
    [HttpPost("checkout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CheckoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CheckoutResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<CheckoutResponse>.ErrorResult("Dữ liệu yêu cầu không hợp lệ."));
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<CheckoutResponse>.ErrorResult("Không thể xác thực người dùng."));
        }

        var result = await _bookingService.CheckoutAsync(userId.Value, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Webhook nhận thông báo chuyển khoản thành công tự động từ PayOS
    /// </summary>
    [HttpPost("payos-webhook")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PayOSWebhook([FromBody] Webhook webhookBody)
    {
        var result = await _bookingService.HandlePayOSWebhookAsync(webhookBody);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Chủ động đồng bộ trạng thái thanh toán từ PayOS (hữu ích cho việc kiểm thử localhost)
    /// </summary>
    [HttpPost("{id:guid}/sync-payos")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SyncPayOSPayment([FromRoute] Guid id)
    {
        var result = await _bookingService.SyncPayOSPaymentAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Khách hàng xem lịch sử các đơn đặt vé của bản thân
    /// </summary>
    [HttpGet("my-bookings")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<BookingHistoryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyBookings()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<List<BookingHistoryResponse>>.ErrorResult("Không thể xác thực người dùng."));
        }

        var result = await _bookingService.GetMyBookingsAsync(userId.Value);
        return Ok(result);
    }

    /// <summary>
    /// Xem chi tiết vé xem phim (kèm mã QR soát vé, rạp, ghế, phòng chiếu)
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBookingById([FromRoute] Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<BookingDetailResponse>.ErrorResult("Không thể xác thực người dùng."));
        }

        var isStaff = User.IsInRole(RoleConstants.Staff) || User.IsInRole("1");
        var result = await _bookingService.GetBookingByIdAsync(userId.Value, id, isStaff);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Nhân viên soát vé / check-in vé tại rạp bằng mã QR hoặc TicketCode
    /// </summary>
    [HttpPost("check-in")]
    [Authorize(Roles = $"{RoleConstants.Staff},1")]
    [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckInTicket([FromBody] CheckInTicketRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<BookingDetailResponse>.ErrorResult("Dữ liệu yêu cầu không hợp lệ."));
        }

        var result = await _bookingService.CheckInTicketAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Giải phóng các ghế giữ tạm thời đã hết hạn 5 phút (kích hoạt thủ công)
    /// </summary>
    [HttpPost("cleanup-expired-seats")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CleanupExpiredSeats([FromServices] BookingMovieTicket_Service.BackgroundJobs.ISeatExpirationJob seatExpirationJob)
    {
        await seatExpirationJob.ReleaseExpiredSeatsAsync();
        return Ok(ApiResponse<string>.SuccessResult("Đã kích hoạt và hoàn tất quét giải phóng các ghế hết hạn giữ."));
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value
                          ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}
