using BookingMovieTicket_Repository.DBContext;
using BookingMovieTicket_Repository.Entities;
using BookingMovieTicket_Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingMovieTicket_Repository.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly BookingMovieTicketSystemDbContext _context;

    public BookingRepository(BookingMovieTicketSystemDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid id)
    {
        return await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);
    }


    public async Task<Booking?> GetBookingWithDetailsAsync(Guid id)
    {
        return await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Payment)
            .Include(b => b.Showtime)
                .ThenInclude(s => s.Event)
                    .ThenInclude(e => e.Venue)
            .Include(b => b.Showtime)
                .ThenInclude(s => s.SeatMap)
            .Include(b => b.BookingSeats)
                .ThenInclude(bs => bs.ShowtimeSeat)
                    .ThenInclude(ss => ss.Seat)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Booking?> GetBookingByOrderCodeAsync(string orderCode)
    {
        return await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Payment)
            .Include(b => b.Showtime)
                .ThenInclude(s => s.Event)
                    .ThenInclude(e => e.Venue)
            .Include(b => b.Showtime)
                .ThenInclude(s => s.SeatMap)
            .Include(b => b.BookingSeats)
                .ThenInclude(bs => bs.ShowtimeSeat)
                    .ThenInclude(ss => ss.Seat)
            .FirstOrDefaultAsync(b => b.Payment != null && b.Payment.TransactionRef == orderCode);
    }

    public async Task<Booking?> GetBookingByQrCodeAsync(string qrCode)
    {
        var cleanCode = qrCode.Trim();
        return await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Payment)
            .Include(b => b.Showtime)
                .ThenInclude(s => s.Event)
                    .ThenInclude(e => e.Venue)
            .Include(b => b.Showtime)
                .ThenInclude(s => s.SeatMap)
            .Include(b => b.BookingSeats)
                .ThenInclude(bs => bs.ShowtimeSeat)
                    .ThenInclude(ss => ss.Seat)
            .FirstOrDefaultAsync(b => b.QrCode == cleanCode || b.Id.ToString() == cleanCode);
    }

    public async Task<List<Booking>> GetBookingsByUserIdAsync(Guid userId)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Payment)
            .Include(b => b.Showtime)
                .ThenInclude(s => s.Event)
                    .ThenInclude(e => e.Venue)
            .Include(b => b.Showtime)
                .ThenInclude(s => s.SeatMap)
            .Include(b => b.BookingSeats)
                .ThenInclude(bs => bs.ShowtimeSeat)
                    .ThenInclude(ss => ss.Seat)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task AddBookingAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
    }

    public Task UpdateBookingAsync(Booking booking)
    {
        _context.Bookings.Update(booking);
        return Task.CompletedTask;
    }

    public async Task AddPaymentAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
    }

    public Task UpdatePaymentAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}

