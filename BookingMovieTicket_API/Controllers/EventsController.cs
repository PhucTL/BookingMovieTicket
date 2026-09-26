using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.Constants;
using BookingMovieTicket.Contracts.DTOs.Event.Request;
using BookingMovieTicket.Contracts.DTOs.Event.Response;
using BookingMovieTicket_Service.Event;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingMovieTicket_API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    /// <summary>
    /// Lấy danh sách phim/sự kiện 
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<EventResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? venueId, [FromQuery] string? search)
    {
        var result = await _eventService.GetAllEventsAsync(venueId, search);
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết một phim/sự kiện kèm rạp và danh sách suất chiếu
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<EventDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EventDetailResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _eventService.GetEventByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Tạo mới phim/sự kiện 
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<EventResponse>.ErrorResult("Dữ liệu yêu cầu không hợp lệ."));
        }

        var result = await _eventService.CreateEventAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật thông tin phim/sự kiện
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<EventResponse>.ErrorResult("Dữ liệu yêu cầu không hợp lệ."));
        }

        var result = await _eventService.UpdateEventAsync(id, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Xóa phim/sự kiện
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleConstants.Staff)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _eventService.DeleteEventAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}

