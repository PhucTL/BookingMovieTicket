using BookingMovieTicket.Contracts.Common;
using BookingMovieTicket.Contracts.DTOs.SeatMap.Request;
using BookingMovieTicket.Contracts.DTOs.SeatMap.Response;
using BookingMovieTicket.Contracts.Enums;
using BookingMovieTicket_Repository.Entities;
using BookingMovieTicket_Repository.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.SeatMap;

public class SeatMapService : ISeatMapService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SeatMapService> _logger;

    public SeatMapService(IUnitOfWork unitOfWork, ILogger<SeatMapService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Lấy chi tiết sơ đồ ghế / cấu trúc phòng chiếu kèm toàn bộ danh sách ghế
    /// </summary>
    public async Task<ApiResponse<SeatMapDetailResponse>> GetSeatMapByIdAsync(Guid id)
    {
        try
        {
            var seatMap = await _unitOfWork.SeatMapRepository.GetSeatMapWithSeatsAsync(id);
            if (seatMap == null)
            {
                return ApiResponse<SeatMapDetailResponse>.ErrorResult("Không tìm thấy sơ đồ ghế với ID được cung cấp.");
            }

            var seats = seatMap.Seats
                .OrderBy(s => s.RowLabel)
                .ThenBy(s => s.Number)
                .Select(s => new SeatItemDto
                {
                    Id = s.Id,
                    RowLabel = s.RowLabel,
                    Number = s.Number,
                    SeatType = s.SeatType,
                    SeatTypeName = Enum.IsDefined(typeof(SeatType), s.SeatType)
                        ? ((SeatType)s.SeatType).ToString()
                        : "Standard"
                }).ToList();

            var response = new SeatMapDetailResponse
            {
                Id = seatMap.Id,
                VenueId = seatMap.VenueId,
                VenueName = seatMap.Venue?.Name ?? string.Empty,
                Name = seatMap.Name,
                Rows = seatMap.Rows,
                Columns = seatMap.Columns,
                TotalSeats = seats.Count > 0 ? seats.Count : seatMap.Rows * seatMap.Columns,
                Seats = seats
            };

            return ApiResponse<SeatMapDetailResponse>.SuccessResult(response, "Lấy thông tin sơ đồ ghế thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin sơ đồ ghế {SeatMapId}.", id);
            return ApiResponse<SeatMapDetailResponse>.ErrorResult("Đã xảy ra lỗi khi lấy thông tin sơ đồ ghế.");
        }
    }

    /// <summary>
    /// Staff tạo phòng chiếu mới 
    /// </summary>
    public async Task<ApiResponse<SeatMapResponse>> CreateSeatMapAsync(CreateSeatMapRequest request)
    {
        try
        {
            var venue = await _unitOfWork.VenueRepository.GetVenueByIdAsync(request.VenueId);
            if (venue == null)
            {
                return ApiResponse<SeatMapResponse>.ErrorResult("Rạp chiếu (VenueId) không tồn tại trong hệ thống.");
            }

            var seatMap = new BookingMovieTicket_Repository.Entities.SeatMap
            {
                Id = Guid.NewGuid(),
                VenueId = request.VenueId,
                Name = request.Name.Trim(),
                Rows = request.Rows,
                Columns = request.Columns
            };

            await _unitOfWork.SeatMapRepository.AddSeatMapAsync(seatMap);
            await _unitOfWork.SaveChangesAsync();

            var response = new SeatMapResponse
            {
                Id = seatMap.Id,
                VenueId = seatMap.VenueId,
                VenueName = venue.Name,
                Name = seatMap.Name,
                Rows = seatMap.Rows,
                Columns = seatMap.Columns,
                TotalSeats = seatMap.Rows * seatMap.Columns
            };

            return ApiResponse<SeatMapResponse>.SuccessResult(response, "Tạo phòng chiếu/sơ đồ ghế mới thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo sơ đồ ghế {SeatMapName}.", request.Name);
            return ApiResponse<SeatMapResponse>.ErrorResult("Đã xảy ra lỗi khi tạo sơ đồ ghế.");
        }
    }

    /// <summary>
    /// Staff sinh hàng loạt ghế tự động cho phòng chiếu dựa trên Rows & Columns
    /// </summary>
    public async Task<ApiResponse<List<SeatResponse>>> BulkGenerateSeatsAsync(Guid seatMapId, BulkGenerateSeatsRequest? request)
    {
        try
        {
            var seatMap = await _unitOfWork.SeatMapRepository.GetSeatMapWithSeatsAsync(seatMapId);
            if (seatMap == null)
            {
                return ApiResponse<List<SeatResponse>>.ErrorResult("Không tìm thấy phòng chiếu/sơ đồ ghế với ID này.");
            }

            // Nếu đã có ghế và request yêu cầu xóa để tạo lại
            if (seatMap.Seats != null && seatMap.Seats.Count > 0)
            {
                if (request?.ClearExisting == true)
                {
                    await _unitOfWork.SeatMapRepository.RemoveSeatsRangeAsync(seatMap.Seats);
                    await _unitOfWork.SaveChangesAsync();
                }
                else
                {
                    return ApiResponse<List<SeatResponse>>.ErrorResult("Phòng chiếu đã có ghế. Vui lòng bật ClearExisting = true nếu muốn sinh lại từ đầu.");
                }
            }

            var newSeats = new List<Seat>();
            var vipRows = request?.VipRowLabels?.Select(r => r.Trim().ToUpper()).ToHashSet() ?? new HashSet<string>();
            var coupleRows = request?.CoupleRowLabels?.Select(r => r.Trim().ToUpper()).ToHashSet() ?? new HashSet<string>();

            // Sinh tự động các hàng từ A, B, C... dựa trên Rows
            for (int r = 0; r < seatMap.Rows; r++)
            {
                char rowChar = (char)('A' + r);
                string rowLabel = rowChar.ToString();

                // Xác định loại ghế mặc định cho hàng này
                short seatType = (short)SeatType.Standard;
                if (coupleRows.Contains(rowLabel))
                {
                    seatType = (short)SeatType.Couple;
                }
                else if (vipRows.Contains(rowLabel))
                {
                    seatType = (short)SeatType.Vip;
                }
                else if (request?.VipRowLabels == null && seatMap.Rows >= 5 && r >= seatMap.Rows / 3 && r <= (2 * seatMap.Rows) / 3)
                {
                    // Nếu không truyền danh sách cụ thể, tự động chọn các hàng đẹp ở giữa làm VIP
                    seatType = (short)SeatType.Vip;
                }

                for (int col = 1; col <= seatMap.Columns; col++)
                {
                    newSeats.Add(new Seat
                    {
                        Id = Guid.NewGuid(),
                        SeatMapId = seatMapId,
                        RowLabel = rowLabel,
                        Number = col,
                        SeatType = seatType
                    });
                }
            }

            await _unitOfWork.SeatMapRepository.AddSeatsRangeAsync(newSeats);
            await _unitOfWork.SaveChangesAsync();

            var response = newSeats.Select(s => new SeatResponse
            {
                Id = s.Id,
                SeatMapId = s.SeatMapId,
                RowLabel = s.RowLabel,
                Number = s.Number,
                SeatType = s.SeatType,
                SeatTypeName = ((SeatType)s.SeatType).ToString()
            }).ToList();

            return ApiResponse<List<SeatResponse>>.SuccessResult(response, $"Đã sinh thành công {newSeats.Count} ghế cho phòng chiếu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi sinh hàng loạt ghế cho SeatMapId={SeatMapId}.", seatMapId);
            return ApiResponse<List<SeatResponse>>.ErrorResult("Đã xảy ra lỗi khi sinh hàng loạt ghế.");
        }
    }

    /// <summary>
    /// Staff cập nhật loại ghế
    /// </summary>
    public async Task<ApiResponse<SeatResponse>> UpdateSeatTypeAsync(Guid seatId, UpdateSeatTypeRequest request)
    {
        try
        {
            var seat = await _unitOfWork.SeatMapRepository.GetSeatByIdAsync(seatId);
            if (seat == null)
            {
                return ApiResponse<SeatResponse>.ErrorResult("Không tìm thấy ghế cần cập nhật.");
            }

            seat.SeatType = (short)request.SeatType;
            await _unitOfWork.SeatMapRepository.UpdateSeatAsync(seat);
            await _unitOfWork.SaveChangesAsync();

            var response = new SeatResponse
            {
                Id = seat.Id,
                SeatMapId = seat.SeatMapId,
                RowLabel = seat.RowLabel,
                Number = seat.Number,
                SeatType = seat.SeatType,
                SeatTypeName = request.SeatType.ToString()
            };

            return ApiResponse<SeatResponse>.SuccessResult(response, $"Đã cập nhật loại ghế thành {request.SeatType}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật loại ghế {SeatId}.", seatId);
            return ApiResponse<SeatResponse>.ErrorResult("Đã xảy ra lỗi khi cập nhật loại ghế.");
        }
    }
}

