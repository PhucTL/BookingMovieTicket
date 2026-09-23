using BookingMovieTicket_Repository.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingMovieTicket_Repository.Interfaces
{
    public interface IAuthenRepository
    {
        /// <summary>
        /// Tìm người dùng theo email (phục vụ đăng nhập Social - Google hoặc Email)
        /// </summary>
        Task<User?> GetUserByEmailAsync(string email);

        /// <summary>
        /// Tìm người dùng theo username (phục vụ đăng nhập truyền thống)
        /// </summary>
        Task<User?> GetUserByUsernameAsync(string username);

        /// <summary>
        /// Lưu người dùng mới khi đăng ký (mặc định trạng thái Active)
        /// </summary>
        Task<User> CreateUserAsync(User user);

        /// <summary>
        /// Tìm người dùng theo Id (lấy từ Claims trong Bearer Token)
        /// </summary>
        Task<User?> GetUserByIdAsync(Guid id);

        /// <summary>
        /// Cập nhật thông tin người dùng (dùng cho verify email, change password, etc.)
        /// </summary>
        Task<User> UpdateUserAsync(User user);
    }
}
