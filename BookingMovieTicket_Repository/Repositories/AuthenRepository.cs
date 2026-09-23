using BookingMovieTicket_Repository.DBContext;
using BookingMovieTicket_Repository.Entities;
using BookingMovieTicket_Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BookingMovieTicket_Repository.Repositories
{
    public class AuthenRepository : IAuthenRepository
    {
        private readonly BookingMovieTicketSystemDbContext context;

        public AuthenRepository(BookingMovieTicketSystemDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Tạo mới người (Register)
        /// </summary>
        public async Task<User> CreateUserAsync(User user)
        {
            if (user.Id == Guid.Empty)
            {
                user.Id = Guid.NewGuid();
            }

            // PostgreSQL (Npgsql) yêu cầu DateTime ở dạng UTC khi ghi vào timestamptz
            if (user.CreatedAt == default)
            {
                user.CreatedAt = DateTime.UtcNow;
            }
            else if (user.CreatedAt.Kind != DateTimeKind.Utc)
            {
                user.CreatedAt = DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc);
            }

            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            return user;
        }

        /// <summary>
        /// Tìm người dùng theo username (Login)
        /// </summary>
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            var normalized = username.Trim().ToLower();

            return await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username != null && u.Username.ToLower() == normalized);
        }

        /// <summary>
        /// Tìm người dùng theo Email
        /// </summary>
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var normalized = email.Trim().ToLower();

            return await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);
        }

        /// <summary>
        /// Cập nhật thông tin người dùng (đổi mật khẩu, cập nhật profile,...)
        /// </summary>
        public async Task<User> UpdateUserAsync(User user)
        {
            context.Users.Update(user);
            await context.SaveChangesAsync();
            return user;
        }
    }
}
