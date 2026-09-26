using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Venue.Request;
using BookingMovieTicket.Contracts.DTOs.Venue.Response;
using BookingMovieTicket_Service.Venue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingMovieTicket_API.Controllers;

[ApiController]
[Route("api/venues")]
public class VenuesController : ControllerBase
{
    private readonly IVenueService _venueService;

    public VenuesController(IVenueService venueService)
    {
        _venueService = venueService;
    }

    /// <summary>
    /// Lấy danh sách rạp chiếu (Người dùng/Khách xem danh sách rạp)
    /// </summary>
    /// Từ khóa tìm kiếm theo tên hoặc địa chỉ (tùy chọn)
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<VenueResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var result = await _venueService.GetAllVenuesAsync(search);
        return Ok(result);
    }

    /// <summary>
    /// Chi tiết 1 rạp chiếu kèm sơ đồ ghế và sự kiện (phim) trực thuộc
    /// </summary>
    /// ID Rạp
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<VenueDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VenueDetailResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _venueService.GetVenueByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Staff/Admin tạo rạp mới
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(typeof(ApiResponse<VenueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VenueResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateVenueRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<VenueResponse>.ErrorResult("Dữ liệu yêu cầu không hợp lệ."));
        }

        var result = await _venueService.CreateVenueAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Staff/Admin chỉnh sửa thông tin rạp
    /// </summary>
    /// ID Rạp
    /// Thông tin cần cập nhật
    [HttpPut("{id}")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(typeof(ApiResponse<VenueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VenueResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVenueRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<VenueResponse>.ErrorResult("Dữ liệu yêu cầu không hợp lệ."));
        }

        var result = await _venueService.UpdateVenueAsync(id, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Staff/Admin xóa rạp
    /// </summary>
    /// ID Rạp
    [HttpDelete("{id}")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _venueService.DeleteVenueAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}

