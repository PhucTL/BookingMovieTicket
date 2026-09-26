using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.Venue.Request;
using BookingMovieTicket.Contracts.DTOs.Venue.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Venue;

public interface IVenueService
{
    /// <summary>
    /// Lấy danh sách toàn bộ rạp (hỗ trợ tìm kiếm theo tên hoặc địa chỉ)
    /// </summary>
    Task<ApiResponse<List<VenueResponse>>> GetAllVenuesAsync(string? search = null);

    /// <summary>
    /// Lấy chi tiết 1 rạp kèm sơ đồ ghế và sự kiện trực thuộc
    /// </summary>
    Task<ApiResponse<VenueDetailResponse>> GetVenueByIdAsync(Guid id);

    /// <summary>
    /// Staff/Admin tạo rạp mới
    /// </summary>
    Task<ApiResponse<VenueResponse>> CreateVenueAsync(CreateVenueRequest request);

    /// <summary>
    /// Staff/Admin chỉnh sửa thông tin rạp
    /// </summary>
    Task<ApiResponse<VenueResponse>> UpdateVenueAsync(Guid id, UpdateVenueRequest request);

    /// <summary>
    /// Staff/Admin xóa rạp
    /// </summary>
    Task<ApiResponse<string>> DeleteVenueAsync(Guid id);
}

