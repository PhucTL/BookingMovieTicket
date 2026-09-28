using BookingMovieTicket_Repository.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingMovieTicket_Repository.Interfaces;

public interface IBookingRepository
{
    /// <summary>
    /// Lấy thông tin cơ bản đơn đặt vé theo Id
    /// </summary>
    Task<Booking?> GetBookingByIdAsync(Guid id);

    /// <summary>
    /// Lấy thông tin chi tiết đơn đặt vé kèm User, Showtime, Event, Venue, SeatMap, BookingSeats, Payment
    /// </summary>
    Task<Booking?> GetBookingWithDetailsAsync(Guid id);

    /// <summary>
    /// Tìm đơn đặt vé theo mã giao dịch / mã đơn hàng PayOS (Payment.TransactionRef)
    /// </summary>
    Task<Booking?> GetBookingByOrderCodeAsync(string orderCode);

    /// <summary>
    /// Tìm đơn đặt vé theo mã QR Code hoặc Ticket Code để check-in soát vé
    /// </summary>
    Task<Booking?> GetBookingByQrCodeAsync(string qrCode);

    /// <summary>
    /// Lấy danh sách lịch sử đặt vé của 1 người dùng (sắp xếp mới nhất)
    /// </summary>
    Task<List<Booking>> GetBookingsByUserIdAsync(Guid userId);

    /// <summary>
    /// Thêm đơn đặt vé mới
    /// </summary>
    Task AddBookingAsync(Booking booking);

    /// <summary>
    /// Cập nhật đơn đặt vé
    /// </summary>
    Task UpdateBookingAsync(Booking booking);

    /// <summary>
    /// Thêm bản ghi thanh toán
    /// </summary>
    Task AddPaymentAsync(Payment payment);

    /// <summary>
    /// Cập nhật bản ghi thanh toán
    /// </summary>
    Task UpdatePaymentAsync(Payment payment);

    /// <summary>
    /// Lưu thay đổi vào cơ sở dữ liệu
    /// </summary>
    Task<int> SaveChangesAsync();
}

