using System;
using System.Collections.Generic;
using System.Text;

namespace BookingMovieTicket_Repository.Interfaces
{
    public interface IUnitOfWork
    {
        /// <summary>
        /// Repository xử lý Authentication
        /// </summary>
        IAuthenRepository AuthenRepository { get; }

        /// <summary>
        /// Repository xử lý Suất chiếu và Ghế
        /// </summary>
        IShowtimeRepository ShowtimeRepository { get; }

        /// <summary>
        /// Repository xử lý Rạp chiếu (Venue)
        /// </summary>
        IVenueRepository VenueRepository { get; }

        /// <summary>
        /// Repository xử lý Phim/Sự kiện (Event)
        /// </summary>
        IEventRepository EventRepository { get; }

        /// <summary>
        /// Lưu tất cả thay đổi vào Database
        /// </summary>
        System.Threading.Tasks.Task<int> SaveChangesAsync();

        /// <summary>
        /// Bắt đầu một Database Transaction
        /// </summary>
        System.Threading.Tasks.Task BeginTransactionAsync();

        /// <summary>
        /// Commit Database Transaction
        /// </summary>
        System.Threading.Tasks.Task CommitTransactionAsync();

        /// <summary>
        /// Rollback Database Transaction
        /// </summary>
        System.Threading.Tasks.Task RollbackTransactionAsync();
    }
}
