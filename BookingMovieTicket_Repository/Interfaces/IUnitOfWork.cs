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
    }
}
