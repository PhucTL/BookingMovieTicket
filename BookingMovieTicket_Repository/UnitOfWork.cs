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
        private VenueRepository? _venueRepository;

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

        public IVenueRepository VenueRepository
        {
            get => _venueRepository ??= new VenueRepository(_context);
        }

        private EventRepository? _eventRepository;
        public IEventRepository EventRepository
        {
            get => _eventRepository ??= new EventRepository(_context);
        }

        private SeatMapRepository? _seatMapRepository;
        public ISeatMapRepository SeatMapRepository
        {
            get => _seatMapRepository ??= new SeatMapRepository(_context);
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
