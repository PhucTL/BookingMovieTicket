using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Event.Request;
using BookingMovieTicket.Contracts.DTOs.Event.Response;
using BookingMovieTicket_Repository.Entities;
using BookingMovieTicket_Repository.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Event;

public class EventService : IEventService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EventService> _logger;

    public EventService(IUnitOfWork unitOfWork, ILogger<EventService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách phim/sự kiện
    /// </summary>
    public async Task<ApiResponse<List<EventResponse>>> GetAllEventsAsync(Guid? venueId = null, string? search = null)
    {
        try
        {
            var events = await _unitOfWork.EventRepository.GetAllEventsAsync(venueId, search);

            var response = events.Select(e => new EventResponse
            {
                Id = e.Id,
                VenueId = e.VenueId,
                VenueName = e.Venue?.Name ?? string.Empty,
                Title = e.Title,
                Description = e.Description,
                DurationMinutes = e.DurationMinutes,
                PosterUrl = e.PosterUrl,
                TotalShowtimes = e.Showtimes?.Count ?? 0
            }).ToList();

            return ApiResponse<List<EventResponse>>.SuccessResult(response, "Lấy danh sách phim/sự kiện thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách phim/sự kiện.");
            return ApiResponse<List<EventResponse>>.ErrorResult("Đã xảy ra lỗi khi lấy danh sách phim/sự kiện.");
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết một phim/sự kiện kèm rạp và danh sách suất chiếu
    /// </summary>
    public async Task<ApiResponse<EventDetailResponse>> GetEventByIdAsync(Guid id)
    {
        try
        {
            var ev = await _unitOfWork.EventRepository.GetEventWithDetailsAsync(id);
            if (ev == null)
            {
                return ApiResponse<EventDetailResponse>.ErrorResult("Không tìm thấy phim/sự kiện với ID được cung cấp.");
            }

            var response = new EventDetailResponse
            {
                Id = ev.Id,
                VenueId = ev.VenueId,
                VenueName = ev.Venue?.Name ?? string.Empty,
                VenueAddress = ev.Venue?.Address,
                Title = ev.Title,
                Description = ev.Description,
                DurationMinutes = ev.DurationMinutes,
                PosterUrl = ev.PosterUrl,
                Showtimes = ev.Showtimes?
                    .OrderBy(s => s.StartTime)
                    .Select(s => new EventShowtimeItemDto
                    {
                        Id = s.Id,
                        StartTime = s.StartTime,
                        BasePriceStandard = s.BasePriceStandard,
                        BasePriceVip = s.BasePriceVip,
                        SeatMapId = s.SeatMapId,
                        SeatMapName = s.SeatMap?.Name ?? string.Empty
                    }).ToList() ?? new()
            };

            return ApiResponse<EventDetailResponse>.SuccessResult(response, "Lấy thông tin chi tiết phim/sự kiện thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin chi tiết phim {EventId}.", id);
            return ApiResponse<EventDetailResponse>.ErrorResult("Đã xảy ra lỗi khi lấy thông tin chi tiết phim/sự kiện.");
        }
    }

    /// <summary>
    /// Staff tạo phim/sự kiện mới
    /// </summary>
    public async Task<ApiResponse<EventResponse>> CreateEventAsync(CreateEventRequest request)
    {
        try
        {
            var venue = await _unitOfWork.VenueRepository.GetVenueByIdAsync(request.VenueId);
            if (venue == null)
            {
                return ApiResponse<EventResponse>.ErrorResult("Rạp chiếu (VenueId) không tồn tại trong hệ thống.");
            }

            var ev = new BookingMovieTicket_Repository.Entities.Event
            {
                Id = Guid.NewGuid(),
                VenueId = request.VenueId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                DurationMinutes = request.DurationMinutes,
                PosterUrl = request.PosterUrl?.Trim()
            };

            await _unitOfWork.EventRepository.AddEventAsync(ev);
            await _unitOfWork.SaveChangesAsync();

            var response = new EventResponse
            {
                Id = ev.Id,
                VenueId = ev.VenueId,
                VenueName = venue.Name,
                Title = ev.Title,
                Description = ev.Description,
                DurationMinutes = ev.DurationMinutes,
                PosterUrl = ev.PosterUrl,
                TotalShowtimes = 0
            };

            return ApiResponse<EventResponse>.SuccessResult(response, "Tạo phim/sự kiện mới thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo mới phim/sự kiện {Title}.", request.Title);
            return ApiResponse<EventResponse>.ErrorResult("Đã xảy ra lỗi khi tạo phim/sự kiện mới.");
        }
    }

    /// <summary>
    /// Staff chỉnh sửa thông tin phim/sự kiện
    /// </summary>
    public async Task<ApiResponse<EventResponse>> UpdateEventAsync(Guid id, UpdateEventRequest request)
    {
        try
        {
            var ev = await _unitOfWork.EventRepository.GetEventByIdAsync(id);
            if (ev == null)
            {
                return ApiResponse<EventResponse>.ErrorResult("Không tìm thấy phim/sự kiện cần cập nhật.");
            }

            var venue = await _unitOfWork.VenueRepository.GetVenueByIdAsync(request.VenueId);
            if (venue == null)
            {
                return ApiResponse<EventResponse>.ErrorResult("Rạp chiếu (VenueId) không tồn tại trong hệ thống.");
            }

            ev.VenueId = request.VenueId;
            ev.Title = request.Title.Trim();
            ev.Description = request.Description?.Trim();
            ev.DurationMinutes = request.DurationMinutes;
            ev.PosterUrl = request.PosterUrl?.Trim();

            await _unitOfWork.EventRepository.UpdateEventAsync(ev);
            await _unitOfWork.SaveChangesAsync();

            var response = new EventResponse
            {
                Id = ev.Id,
                VenueId = ev.VenueId,
                VenueName = venue.Name,
                Title = ev.Title,
                Description = ev.Description,
                DurationMinutes = ev.DurationMinutes,
                PosterUrl = ev.PosterUrl,
                TotalShowtimes = ev.Showtimes?.Count ?? 0
            };

            return ApiResponse<EventResponse>.SuccessResult(response, "Cập nhật phim/sự kiện thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật phim/sự kiện {EventId}.", id);
            return ApiResponse<EventResponse>.ErrorResult("Đã xảy ra lỗi khi cập nhật phim/sự kiện.");
        }
    }

    /// <summary>
    /// Staff xóa phim/sự kiện
    /// </summary>
    public async Task<ApiResponse<string>> DeleteEventAsync(Guid id)
    {
        try
        {
            var ev = await _unitOfWork.EventRepository.GetEventWithDetailsAsync(id);
            if (ev == null)
            {
                return ApiResponse<string>.ErrorResult("Không tìm thấy phim/sự kiện cần xóa.");
            }

            if (ev.Showtimes != null && ev.Showtimes.Count > 0)
            {
                return ApiResponse<string>.ErrorResult($"Không thể xóa phim '{ev.Title}' vì đang có {ev.Showtimes.Count} suất chiếu liên kết. Vui lòng xóa các suất chiếu trước.");
            }

            await _unitOfWork.EventRepository.DeleteEventAsync(ev);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<string>.SuccessResult($"Đã xóa phim/sự kiện '{ev.Title}' thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa phim/sự kiện {EventId}.", id);
            return ApiResponse<string>.ErrorResult("Đã xảy ra lỗi khi xóa phim/sự kiện.");
        }
    }
}

