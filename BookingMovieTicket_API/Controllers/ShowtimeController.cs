using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.Constants;
using BookingMovieTicket.Contracts.DTOs.Showtime.Request;
using BookingMovieTicket.Contracts.DTOs.Showtime.Response;
using BookingMovieTicket_Service.Showtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookingMovieTicket_API.Controllers;

[ApiController]
[Route("api/showtimes")]
[Route("api/[controller]")]
public class ShowtimeController : ControllerBase
{
    private readonly IShowtimeService _showtimeService;

    public ShowtimeController(IShowtimeService showtimeService)
    {
        _showtimeService = showtimeService;
    }

    /// <summary>
    /// Lấy danh sách suất chiếu theo phim và/hoặc ngày chiếu
    /// </summary>
    /// <param name="eventId">Mã phim/sự kiện (tùy chọn)</param>
    /// <param name="date">Ngày chiếu cần xem (định dạng YYYY-MM-DD, tùy chọn)</param>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<ShowtimeItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? eventId, [FromQuery] DateTime? date)
    {
        var result = await _showtimeService.GetShowtimesAsync(eventId, date);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết 1 suất chiếu
    /// </summary>
    /// <param name="id">Mã suất chiếu (Guid)</param>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ShowtimeItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ShowtimeItemResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _showtimeService.GetShowtimeByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Lấy sơ đồ ghế và trạng thái các ghế theo Suất chiếu (Showtime)
    /// </summary>
    /// <param name="id">Mã suất chiếu (ShowtimeId)</param>
    [HttpGet("{id}/seats")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ShowtimeSeatMapResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ShowtimeSeatMapResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSeatMap(Guid id)
    {
        // Lấy userId nếu người dùng đang đăng nhập (để đánh dấu ghế nào do chính họ đang giữ)
        Guid? currentUserId = null;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value
                          ?? User.FindFirst("userId")?.Value;

        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedId))
        {
            currentUserId = parsedId;
        }

        var result = await _showtimeService.GetSeatMapByShowtimeIdAsync(id, currentUserId);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Staff tạo suất chiếu mới (Hệ thống tự động sinh toàn bộ ShowtimeSeat từ phòng chiếu và cấu hình giá vé)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(ApiResponse<ShowtimeItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ShowtimeItemResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateShowtimeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<ShowtimeItemResponse>.ErrorResult("Dữ liệu yêu cầu không hợp lệ."));
        }

        var result = await _showtimeService.CreateShowtimeAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Staff sửa giờ chiếu, giá vé của suất chiếu
    /// </summary>
    /// <param name="id">Mã định danh suất chiếu (Guid)</param>
    /// <param name="request">Thông tin cập nhật</param>
    [HttpPut("{id}")]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(ApiResponse<ShowtimeItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ShowtimeItemResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShowtimeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<ShowtimeItemResponse>.ErrorResult("Dữ liệu yêu cầu không hợp lệ."));
        }

        var result = await _showtimeService.UpdateShowtimeAsync(id, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Staff hủy/xóa suất chiếu
    /// </summary>
    /// <param name="id">Mã định danh suất chiếu (Guid)</param>
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _showtimeService.DeleteShowtimeAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}

