using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.Constants;
using BookingMovieTicket.Contracts.DTOs.SeatMap.Request;
using BookingMovieTicket.Contracts.DTOs.SeatMap.Response;
using BookingMovieTicket_Service.SeatMap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingMovieTicket_API.Controllers;

[ApiController]
[Route("api/seatmaps")]
public class SeatMapsController : ControllerBase
{
    private readonly ISeatMapService _seatMapService;

    public SeatMapsController(ISeatMapService seatMapService)
    {
        _seatMapService = seatMapService;
    }

    /// <summary>
    /// Xem cấu trúc phòng chiếu và sơ đồ ghế
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SeatMapDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SeatMapDetailResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _seatMapService.GetSeatMapByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Staff tạo phòng chiếu mới
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(ApiResponse<SeatMapResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SeatMapResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateSeatMapRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<SeatMapResponse>.ErrorResult("Dữ liệu yêu cầu không hợp lệ."));
        }

        var result = await _seatMapService.CreateSeatMapAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Staff sinh hàng loạt ghế tự động cho phòng chiếu
    /// </summary>
    [HttpPost("{id}/seats/bulk")]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(ApiResponse<List<SeatResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<SeatResponse>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BulkGenerateSeats(Guid id, [FromBody] BulkGenerateSeatsRequest? request)
    {
        var result = await _seatMapService.BulkGenerateSeatsAsync(id, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}

