using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.Constants;
using BookingMovieTicket.Contracts.DTOs.SeatMap.Request;
using BookingMovieTicket.Contracts.DTOs.SeatMap.Response;
using BookingMovieTicket_Service.SeatMap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BookingMovieTicket_API.Controllers;

[ApiController]
[Route("api/seats")]
public class SeatsController : ControllerBase
{
    private readonly ISeatMapService _seatMapService;

    public SeatsController(ISeatMapService seatMapService)
    {
        _seatMapService = seatMapService;
    }

    /// <summary>
    /// Staff sửa loại ghế của 1 ghế cụ thể (ví dụ: đổi Standard sang VIP hoặc Couple)
    /// </summary>
    /// <param name="id">Mã định danh ghế (Guid)</param>
    /// <param name="request">Loại ghế mới (0 = Standard, 1 = Vip, 2 = Couple)</param>
    [HttpPut("{id}")]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(ApiResponse<SeatResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SeatResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSeatType(Guid id, [FromBody] UpdateSeatTypeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<SeatResponse>.ErrorResult("Dữ liệu yêu cầu không hợp lệ."));
        }

        var result = await _seatMapService.UpdateSeatTypeAsync(id, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}

