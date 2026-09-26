using BookingMovieTicket_Repository.DBContext;
using BookingMovieTicket_Repository.Entities;
using BookingMovieTicket_Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingMovieTicket_Repository.Repositories;

public class ShowtimeRepository : IShowtimeRepository
{
    private readonly BookingMovieTicketSystemDbContext _context;

    public ShowtimeRepository(BookingMovieTicketSystemDbContext context)
    {
        _context = context;
    }

    public async Task<Showtime?> GetShowtimeWithDetailsAsync(Guid showtimeId)
    {
        return await _context.Showtimes
            .AsNoTracking()
            .Include(s => s.Event)
            .Include(s => s.SeatMap)
                .ThenInclude(sm => sm.Venue)
            .Include(s => s.ShowtimeSeats)
                .ThenInclude(ss => ss.Seat)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);
    }

    public async Task<Showtime?> GetShowtimeByIdAsync(Guid showtimeId)
    {
        return await _context.Showtimes
            .AsNoTracking()
            .Include(s => s.Event)
            .Include(s => s.SeatMap)
                .ThenInclude(sm => sm.Venue)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);
    }

    public async Task<List<ShowtimeSeat>> GetShowtimeSeatsAsync(Guid showtimeId)
    {
        return await _context.ShowtimeSeats
            .AsNoTracking()
            .Include(ss => ss.Seat)
            .Where(ss => ss.ShowtimeId == showtimeId)
            .OrderBy(ss => ss.Seat.RowLabel)
            .ThenBy(ss => ss.Seat.Number)
            .ToListAsync();
    }

    public async Task<ShowtimeSeat?> GetShowtimeSeatByIdAsync(Guid showtimeSeatId)
    {
        return await _context.ShowtimeSeats
            .Include(ss => ss.Seat)
            .FirstOrDefaultAsync(ss => ss.Id == showtimeSeatId);
    }

    public async Task<List<ShowtimeSeat>> GetShowtimeSeatsByIdsAsync(Guid showtimeId, IEnumerable<Guid> seatIds)
    {
        return await _context.ShowtimeSeats
            .Include(ss => ss.Seat)
            .Where(ss => ss.ShowtimeId == showtimeId && (seatIds.Contains(ss.SeatId) || seatIds.Contains(ss.Id)))
            .ToListAsync();
    }

    public Task UpdateShowtimeSeatAsync(ShowtimeSeat seat)
    {
        _context.ShowtimeSeats.Update(seat);
        return Task.CompletedTask;
    }

    public Task UpdateShowtimeSeatsAsync(IEnumerable<ShowtimeSeat> seats)
    {
        _context.ShowtimeSeats.UpdateRange(seats);
        return Task.CompletedTask;
    }

    public async Task<List<ShowtimeSeat>> GetExpiredHeldSeatsAsync(DateTime cutoffTime, short status = 1)
    {
        return await _context.ShowtimeSeats
            .Include(ss => ss.Seat)
            .Where(ss => ss.Status == status
                         && ss.HeldUntil != null
                         && ss.HeldUntil <= cutoffTime)
            .ToListAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}

