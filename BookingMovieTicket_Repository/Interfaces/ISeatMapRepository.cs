using BookingMovieTicket_Repository.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingMovieTicket_Repository.Interfaces;

public interface ISeatMapRepository
{
    /// <summary>
    /// Lấy danh sách tất cả các sơ đồ ghế / phòng chiếu thuộc về một rạp chiếu cụ thể
    /// </summary>
    Task<List<SeatMap>> GetSeatMapsByVenueIdAsync(Guid venueId);

    /// <summary>
    /// Lấy thông tin cơ bản của một sơ đồ ghế theo Id
    /// </summary>
    Task<SeatMap?> GetSeatMapByIdAsync(Guid id);

    /// <summary>
    /// Lấy thông tin chi tiết sơ đồ ghế kèm theo toàn bộ danh sách ghế ngồi trực thuộc
    /// </summary>
    Task<SeatMap?> GetSeatMapWithSeatsAsync(Guid id);

    /// <summary>
    /// Thêm một sơ đồ ghế / phòng chiếu mới vào cơ sở dữ liệu
    /// </summary>
    Task AddSeatMapAsync(SeatMap seatMap);

    /// <summary>
    /// Cập nhật thông tin của sơ đồ ghế / phòng chiếu
    /// </summary>
    Task UpdateSeatMapAsync(SeatMap seatMap);

    /// <summary>
    /// Lấy thông tin chi tiết một ghế ngồi cụ thể theo Id
    /// </summary>
    Task<Seat?> GetSeatByIdAsync(Guid seatId);

    /// <summary>
    /// Cập nhật thông tin của một ghế ngồi
    /// </summary>
    Task UpdateSeatAsync(Seat seat);

    /// <summary>
    /// Thêm hàng loạt ghế ngồi vào cơ sở dữ liệu 
    /// </summary>
    Task AddSeatsRangeAsync(IEnumerable<Seat> seats);

    /// <summary>
    /// Xóa hàng loạt ghế ngồi khỏi sơ đồ ghế 
    /// </summary>
    Task RemoveSeatsRangeAsync(IEnumerable<Seat> seats);

    /// <summary>
    /// Lưu tất cả thay đổi vào cơ sở dữ liệu
    /// </summary>
    Task<int> SaveChangesAsync();
}

