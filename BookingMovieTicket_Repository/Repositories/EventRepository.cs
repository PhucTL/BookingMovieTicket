using BookingMovieTicket_Repository.DBContext;
using BookingMovieTicket_Repository.Entities;
using BookingMovieTicket_Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingMovieTicket_Repository.Repositories;

public class EventRepository : IEventRepository
{
    private readonly BookingMovieTicketSystemDbContext _context;

    public EventRepository(BookingMovieTicketSystemDbContext context)
    {
        _context = context;
    }

    public async Task<List<Event>> GetAllEventsAsync(Guid? venueId = null, string? search = null)
    {
        var query = _context.Events
            .AsNoTracking()
            .Include(e => e.Venue)
            .Include(e => e.Showtimes)
            .AsQueryable();

        if (venueId.HasValue)
        {
            query = query.Where(e => e.VenueId == venueId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(searchLower));
        }

        return await query.OrderBy(e => e.Title).ToListAsync();
    }

    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        return await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Event?> GetEventWithDetailsAsync(Guid id)
    {
        return await _context.Events
            .AsNoTracking()
            .Include(e => e.Venue)
            .Include(e => e.Showtimes)
                .ThenInclude(s => s.SeatMap)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task AddEventAsync(Event ev)
    {
        await _context.Events.AddAsync(ev);
    }

    public Task UpdateEventAsync(Event ev)
    {
        _context.Events.Update(ev);
        return Task.CompletedTask;
    }

    public Task DeleteEventAsync(Event ev)
    {
        _context.Events.Remove(ev);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}

