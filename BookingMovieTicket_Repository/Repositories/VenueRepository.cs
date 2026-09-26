using BookingMovieTicket_Repository.DBContext;
using BookingMovieTicket_Repository.Entities;
using BookingMovieTicket_Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingMovieTicket_Repository.Repositories;

public class VenueRepository : IVenueRepository
{
    private readonly BookingMovieTicketSystemDbContext _context;

    public VenueRepository(BookingMovieTicketSystemDbContext context)
    {
        _context = context;
    }

    public async Task<List<Venue>> GetAllVenuesAsync(string? search = null)
    {
        var query = _context.Venues
            .AsNoTracking()
            .Include(v => v.SeatMaps)
            .Include(v => v.Events)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(v => v.Name.ToLower().Contains(searchLower)
                                  || (v.Address != null && v.Address.ToLower().Contains(searchLower)));
        }

        return await query.OrderByDescending(v => v.CreatedAt).ToListAsync();
    }

    public async Task<Venue?> GetVenueByIdAsync(Guid id)
    {
        return await _context.Venues.FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Venue?> GetVenueWithDetailsAsync(Guid id)
    {
        return await _context.Venues
            .AsNoTracking()
            .Include(v => v.SeatMaps)
            .Include(v => v.Events)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
    {
        var trimmedName = name.Trim().ToLower();
        var query = _context.Venues.Where(v => v.Name.ToLower() == trimmedName);

        if (excludeId.HasValue)
        {
            query = query.Where(v => v.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task AddVenueAsync(Venue venue)
    {
        await _context.Venues.AddAsync(venue);
    }

    public Task UpdateVenueAsync(Venue venue)
    {
        _context.Venues.Update(venue);
        return Task.CompletedTask;
    }

    public Task DeleteVenueAsync(Venue venue)
    {
        _context.Venues.Remove(venue);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}

