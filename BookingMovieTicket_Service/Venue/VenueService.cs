using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Venue.Request;
using BookingMovieTicket.Contracts.DTOs.Venue.Response;
using BookingMovieTicket_Repository.Entities;
using BookingMovieTicket_Repository.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Venue;

public class VenueService : IVenueService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VenueService> _logger;

    public VenueService(IUnitOfWork unitOfWork, ILogger<VenueService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ rạp (hỗ trợ tìm kiếm theo tên hoặc địa chỉ)
    /// </summary>
    public async Task<ApiResponse<List<VenueResponse>>> GetAllVenuesAsync(string? search = null)
    {
        try
        {
            var venues = await _unitOfWork.VenueRepository.GetAllVenuesAsync(search);

            var response = venues.Select(v => new VenueResponse
            {
                Id = v.Id,
                Name = v.Name,
                Address = v.Address,
                CreatedAt = v.CreatedAt,
                TotalSeatMaps = v.SeatMaps?.Count ?? 0,
                TotalEvents = v.Events?.Count ?? 0
            }).ToList();

            return ApiResponse<List<VenueResponse>>.SuccessResult(response, "Lấy danh sách rạp thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách rạp.");
            return ApiResponse<List<VenueResponse>>.ErrorResult("Đã xảy ra lỗi khi lấy danh sách rạp.");
        }
    }

    /// <summary>
    /// Lấy chi tiết 1 rạp kèm sơ đồ ghế và sự kiện trực thuộc
    /// </summary>
    public async Task<ApiResponse<VenueDetailResponse>> GetVenueByIdAsync(Guid id)
    {
        try
        {
            var venue = await _unitOfWork.VenueRepository.GetVenueWithDetailsAsync(id);
            if (venue == null)
            {
                return ApiResponse<VenueDetailResponse>.ErrorResult("Không tìm thấy rạp với ID được cung cấp.");
            }

            var response = new VenueDetailResponse
            {
                Id = venue.Id,
                Name = venue.Name,
                Address = venue.Address,
                CreatedAt = venue.CreatedAt,
                SeatMaps = venue.SeatMaps?.Select(sm => new VenueSeatMapItemDto
                {
                    Id = sm.Id,
                    Name = sm.Name,
                    Rows = sm.Rows,
                    Columns = sm.Columns
                }).ToList() ?? new(),
                Events = venue.Events?.Select(e => new VenueEventItemDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    DurationMinutes = e.DurationMinutes,
                    PosterUrl = e.PosterUrl
                }).ToList() ?? new()
            };

            return ApiResponse<VenueDetailResponse>.SuccessResult(response, "Lấy chi tiết rạp thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin chi tiết rạp {VenueId}.", id);
            return ApiResponse<VenueDetailResponse>.ErrorResult("Đã xảy ra lỗi khi lấy thông tin chi tiết rạp.");
        }
    }

    /// <summary>
    /// Staff/Admin tạo rạp mới
    /// </summary>
    public async Task<ApiResponse<VenueResponse>> CreateVenueAsync(CreateVenueRequest request)
    {
        try
        {
            var trimmedName = request.Name.Trim();
            if (await _unitOfWork.VenueRepository.ExistsByNameAsync(trimmedName))
            {
                return ApiResponse<VenueResponse>.ErrorResult($"Tên rạp '{trimmedName}' đã tồn tại trong hệ thống.");
            }

            var venue = new BookingMovieTicket_Repository.Entities.Venue
            {
                Id = Guid.NewGuid(),
                Name = trimmedName,
                Address = request.Address?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.VenueRepository.AddVenueAsync(venue);
            await _unitOfWork.SaveChangesAsync();

            var response = new VenueResponse
            {
                Id = venue.Id,
                Name = venue.Name,
                Address = venue.Address,
                CreatedAt = venue.CreatedAt,
                TotalSeatMaps = 0,
                TotalEvents = 0
            };

            return ApiResponse<VenueResponse>.SuccessResult(response, "Tạo rạp mới thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo mới rạp {VenueName}.", request.Name);
            return ApiResponse<VenueResponse>.ErrorResult("Đã xảy ra lỗi khi tạo rạp mới.");
        }
    }

    /// <summary>
    /// Staff/Admin chỉnh sửa thông tin rạp
    /// </summary>
    public async Task<ApiResponse<VenueResponse>> UpdateVenueAsync(Guid id, UpdateVenueRequest request)
    {
        try
        {
            var venue = await _unitOfWork.VenueRepository.GetVenueByIdAsync(id);
            if (venue == null)
            {
                return ApiResponse<VenueResponse>.ErrorResult("Không tìm thấy rạp cần cập nhật.");
            }

            var trimmedName = request.Name.Trim();
            if (await _unitOfWork.VenueRepository.ExistsByNameAsync(trimmedName, id))
            {
                return ApiResponse<VenueResponse>.ErrorResult($"Tên rạp '{trimmedName}' đã được sử dụng bởi rạp khác.");
            }

            venue.Name = trimmedName;
            venue.Address = request.Address?.Trim();

            await _unitOfWork.VenueRepository.UpdateVenueAsync(venue);
            await _unitOfWork.SaveChangesAsync();

            var response = new VenueResponse
            {
                Id = venue.Id,
                Name = venue.Name,
                Address = venue.Address,
                CreatedAt = venue.CreatedAt,
                TotalSeatMaps = venue.SeatMaps?.Count ?? 0,
                TotalEvents = venue.Events?.Count ?? 0
            };

            return ApiResponse<VenueResponse>.SuccessResult(response, "Cập nhật rạp thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật rạp {VenueId}.", id);
            return ApiResponse<VenueResponse>.ErrorResult("Đã xảy ra lỗi khi cập nhật rạp.");
        }
    }

    /// <summary>
    /// Staff/Admin xóa rạp
    /// </summary>
    public async Task<ApiResponse<string>> DeleteVenueAsync(Guid id)
    {
        try
        {
            var venue = await _unitOfWork.VenueRepository.GetVenueWithDetailsAsync(id);
            if (venue == null)
            {
                return ApiResponse<string>.ErrorResult("Không tìm thấy rạp cần xóa.");
            }

            if (venue.Events != null && venue.Events.Count > 0)
            {
                return ApiResponse<string>.ErrorResult($"Không thể xóa rạp '{venue.Name}' vì đang có {venue.Events.Count} sự kiện/phim liên kết.");
            }

            if (venue.SeatMaps != null && venue.SeatMaps.Count > 0)
            {
                return ApiResponse<string>.ErrorResult($"Không thể xóa rạp '{venue.Name}' vì đang có {venue.SeatMaps.Count} sơ đồ ghế/phòng chiếu liên kết.");
            }

            await _unitOfWork.VenueRepository.DeleteVenueAsync(venue);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<string>.SuccessResult($"Đã xóa rạp '{venue.Name}' thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa rạp {VenueId}.", id);
            return ApiResponse<string>.ErrorResult("Đã xảy ra lỗi khi xóa rạp.");
        }
    }
}

