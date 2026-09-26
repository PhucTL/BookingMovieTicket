using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Showtime.Response;
using BookingMovieTicket_Service.Showtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookingMovieTicket_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShowtimeController : ControllerBase
{
    private readonly IShowtimeService _showtimeService;

    public ShowtimeController(IShowtimeService showtimeService)
    {
        _showtimeService = showtimeService;
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
}

