using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BookingMovieTicket_API.Hubs;

public class SeatHub : Hub
{
    private readonly ILogger<SeatHub> _logger;

    public SeatHub(ILogger<SeatHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Client tham gia vào nhóm phòng theo dõi sơ đồ ghế của Suất chiếu cụ thể
    /// </summary>
    public async Task JoinShowtimeGroup(string showtimeId)
    {
        if (string.IsNullOrWhiteSpace(showtimeId)) return;

        var groupName = $"Showtime_{showtimeId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} đã tham gia nhóm {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Client rời khỏi nhóm phòng theo dõi sơ đồ ghế của Suất chiếu
    /// </summary>
    public async Task LeaveShowtimeGroup(string showtimeId)
    {
        if (string.IsNullOrWhiteSpace(showtimeId)) return;

        var groupName = $"Showtime_{showtimeId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} đã rời khỏi nhóm {GroupName}", Context.ConnectionId, groupName);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client SignalR đã kết nối: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client SignalR đã ngắt kết nối: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

