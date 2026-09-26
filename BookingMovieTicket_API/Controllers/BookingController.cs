using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Booking.Request;
using BookingMovieTicket.Contracts.DTOs.Booking.Response;
using BookingMovieTicket_Service.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookingMovieTicket_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Giữ ghế tạm thời trong 5 phút để chuẩn bị thanh toán (Bảo vệ bằng Redis Distributed Lock)
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

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value
                          ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<HoldSeatsResponse>.ErrorResult("Không thể xác thực người dùng."));
        }

        var result = await _bookingService.HoldSeatsAsync(userId, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Người dùng chủ động hủy/nhả các ghế đang giữ
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

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value
                          ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<string>.ErrorResult("Không thể xác thực người dùng."));
        }

        var result = await _bookingService.ReleaseSeatsAsync(userId, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Kích hoạt thủ công tiến trình quét và giải phóng các ghế giữ tạm thời đã hết hạn 5 phút (Mặc định được Hangfire tự động chạy ngầm mỗi phút)
    /// </summary>
    [HttpPost("cleanup-expired-seats")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CleanupExpiredSeats([FromServices] BookingMovieTicket_Service.BackgroundJobs.ISeatExpirationJob seatExpirationJob)
    {
        await seatExpirationJob.ReleaseExpiredSeatsAsync();
        return Ok(ApiResponse<string>.SuccessResult("Đã kích hoạt và hoàn tất quét giải phóng các ghế hết hạn giữ."));
    }
}

