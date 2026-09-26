using BookingMovieTicket_Repository.DBContext;
using BookingMovieTicket_Repository.Entities;
using BookingMovieTicket_Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingMovieTicket_Repository.Repositories;

public class SeatMapRepository : ISeatMapRepository
{
    private readonly BookingMovieTicketSystemDbContext _context;

    public SeatMapRepository(BookingMovieTicketSystemDbContext context)
    {
        _context = context;
    }

    public async Task<List<SeatMap>> GetSeatMapsByVenueIdAsync(Guid venueId)
    {
        return await _context.SeatMaps
            .AsNoTracking()
            .Include(sm => sm.Venue)
            .Include(sm => sm.Seats)
            .Where(sm => sm.VenueId == venueId)
            .OrderBy(sm => sm.Name)
            .ToListAsync();
    }

    public async Task<SeatMap?> GetSeatMapByIdAsync(Guid id)
    {
        return await _context.SeatMaps
            .Include(sm => sm.Venue)
            .FirstOrDefaultAsync(sm => sm.Id == id);
    }

    public async Task<SeatMap?> GetSeatMapWithSeatsAsync(Guid id)
    {
        return await _context.SeatMaps
            .AsNoTracking()
            .Include(sm => sm.Venue)
            .Include(sm => sm.Seats)
            .FirstOrDefaultAsync(sm => sm.Id == id);
    }

    public async Task AddSeatMapAsync(SeatMap seatMap)
    {
        await _context.SeatMaps.AddAsync(seatMap);
    }

    public Task UpdateSeatMapAsync(SeatMap seatMap)
    {
        _context.SeatMaps.Update(seatMap);
        return Task.CompletedTask;
    }

    public async Task<Seat?> GetSeatByIdAsync(Guid seatId)
    {
        return await _context.Seats
            .Include(s => s.SeatMap)
            .FirstOrDefaultAsync(s => s.Id == seatId);
    }

    public Task UpdateSeatAsync(Seat seat)
    {
        _context.Seats.Update(seat);
        return Task.CompletedTask;
    }

    public async Task AddSeatsRangeAsync(IEnumerable<Seat> seats)
    {
        await _context.Seats.AddRangeAsync(seats);
    }

    public Task RemoveSeatsRangeAsync(IEnumerable<Seat> seats)
    {
        _context.Seats.RemoveRange(seats);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}

