using BookingMovieTicket_Repository.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingMovieTicket_Repository.Interfaces;

public interface IVenueRepository
{
    /// <summary>
    /// Lấy danh sách rạp (tìm kiếm theo tên hoặc địa chỉ)
    /// </summary>
    Task<List<Venue>> GetAllVenuesAsync(string? search = null);

    /// <summary>
    /// Lấy thông tin cơ bản của 1 rạp theo Id
    /// </summary>
    Task<Venue?> GetVenueByIdAsync(Guid id);

    /// <summary>
    /// Lấy thông tin chi tiết của 1 rạp kèm danh sách sơ đồ ghế (phòng chiếu) và sự kiện (phim)
    /// </summary>
    Task<Venue?> GetVenueWithDetailsAsync(Guid id);

    /// <summary>
    /// Kiểm tra tên rạp đã tồn tại hay chưa (dùng khi tạo hoặc cập nhật)
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);

    /// <summary>
    /// Thêm rạp mới
    /// </summary>
    Task AddVenueAsync(Venue venue);

    /// <summary>
    /// Cập nhật thông tin rạp
    /// </summary>
    Task UpdateVenueAsync(Venue venue);

    /// <summary>
    /// Xóa rạp
    /// </summary>
    Task DeleteVenueAsync(Venue venue);

    /// <summary>
    /// Lưu thay đổi
    /// </summary>
    Task<int> SaveChangesAsync();
}

