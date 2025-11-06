using Microsoft.AspNetCore.SignalR;

namespace BookGrid.Hubs;

public class BookingHub : Hub
{
    private readonly ILogger<BookingHub> _logger;

    public BookingHub(ILogger<BookingHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinRoomGroup(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{roomId}");
        _logger.LogInformation("Client {ConnectionId} joined room group {RoomId}", Context.ConnectionId, roomId);
    }

    public async Task LeaveRoomGroup(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Room_{roomId}");
        _logger.LogInformation("Client {ConnectionId} left room group {RoomId}", Context.ConnectionId, roomId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected: {Exception}", Context.ConnectionId, exception?.Message);
        await base.OnDisconnectedAsync(exception);
    }

    // Method to notify all clients about booking updates
    public static async Task NotifyBookingUpdate(IHubContext<BookingHub> hubContext, int roomId)
    {
        await hubContext.Clients.Group($"Room_{roomId}").SendAsync("BookingUpdated", roomId);
        await hubContext.Clients.All.SendAsync("GlobalBookingUpdate", roomId);
    }
}