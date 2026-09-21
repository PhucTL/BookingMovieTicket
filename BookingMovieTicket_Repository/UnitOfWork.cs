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
    }
}
