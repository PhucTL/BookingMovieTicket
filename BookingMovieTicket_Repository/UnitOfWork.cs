using BookingMovieTicket_Repository.DBContext;
using BookingMovieTicket_Repository.Interfaces;
using BookingMovieTicket_Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingMovieTicket_Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BookingMovieTicketSystemDbContext _context;
        private AuthenRepository? _authenRepository;
        private ShowtimeRepository? _showtimeRepository;

        public UnitOfWork()
        {
            _context = new BookingMovieTicketSystemDbContext();
        }

        public UnitOfWork(BookingMovieTicketSystemDbContext context)
        {
            _context = context;
        }

        public IAuthenRepository AuthenRepository
        {
            get => _authenRepository ??= new AuthenRepository(_context);
        }

        public IShowtimeRepository ShowtimeRepository
        {
            get => _showtimeRepository ??= new ShowtimeRepository(_context);
        }

        public async System.Threading.Tasks.Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async System.Threading.Tasks.Task BeginTransactionAsync()
        {
            await _context.Database.BeginTransactionAsync();
        }

        public async System.Threading.Tasks.Task CommitTransactionAsync()
        {
            await _context.Database.CommitTransactionAsync();
        }

        public async System.Threading.Tasks.Task RollbackTransactionAsync()
        {
            await _context.Database.RollbackTransactionAsync();
        }
    }
}
