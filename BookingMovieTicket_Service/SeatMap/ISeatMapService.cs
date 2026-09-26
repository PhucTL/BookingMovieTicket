using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.SeatMap.Request;
using BookingMovieTicket.Contracts.DTOs.SeatMap.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.SeatMap;

public interface ISeatMapService
{
    /// <summary>
    /// Lấy chi tiết sơ đồ ghế / cấu trúc phòng chiếu kèm toàn bộ danh sách ghế
    /// </summary>
    Task<ApiResponse<SeatMapDetailResponse>> GetSeatMapByIdAsync(Guid id);

    /// <summary>
    /// Staff tạo phòng chiếu mới 
    /// </summary>
    Task<ApiResponse<SeatMapResponse>> CreateSeatMapAsync(CreateSeatMapRequest request);

    /// <summary>
    /// Staff sinh hàng loạt ghế tự động cho phòng chiếu dựa trên Rows & Columns
    /// </summary>
    Task<ApiResponse<List<SeatResponse>>> BulkGenerateSeatsAsync(Guid seatMapId, BulkGenerateSeatsRequest? request);

    /// <summary>
    /// Staff cập nhật loại ghế
    /// </summary>
    Task<ApiResponse<SeatResponse>> UpdateSeatTypeAsync(Guid seatId, UpdateSeatTypeRequest request);
}

