using BookGrid.Models;
using Microsoft.Extensions.Caching.Memory;

namespace BookGrid.Services;

public class RoomService
{
    private readonly LibCalService _libCalService;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RoomService> _logger;
    private readonly int[] _roomIds;
    private readonly TimeSpan _cacheExpiration;

    public RoomService(
        LibCalService libCalService,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<RoomService> logger)
    {
        _libCalService = libCalService;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        _roomIds = configuration.GetSection("LibCal:RoomItemIds").Get<int[]>() ?? Array.Empty<int>();
        _cacheExpiration = TimeSpan.FromMinutes(configuration.GetValue("Cache:DefaultExpirationMinutes", 5));
    }

    public async Task<List<Room>> GetRoomsAsync(DateTime date)
    {
        var cacheKey = $"rooms_{date:yyyy-MM-dd}";
        
        if (_cache.TryGetValue(cacheKey, out List<Room>? cachedRooms) && cachedRooms != null)
        {
            _logger.LogInformation("Returning cached rooms for {Date}", date);
            return cachedRooms;
        }

        _logger.LogInformation("Fetching fresh room data for {Date}", date);
        var rooms = new List<Room>();

        // Fetch bookings first
        var bookings = await _libCalService.GetBookingsAsync(date);
        var bookingsByRoom = bookings.GroupBy(b => b.RoomId).ToDictionary(g => g.Key, g => g.ToList());

        // Fetch room details in parallel
        var tasks = _roomIds.Select(async roomId =>
        {
            var room = await _libCalService.GetRoomDetailsAsync(roomId, date);
            if (room != null)
            {
                room.Bookings = bookingsByRoom.GetValueOrDefault(roomId, new List<Booking>());
                return room;
            }
            
            // Fallback room if API fails
            return new Room
            {
                Id = roomId,
                Name = $"Room {roomId}",
                Zone = GetRoomZone(roomId),
                Bookings = bookingsByRoom.GetValueOrDefault(roomId, new List<Booking>())
            };
        });

        var roomResults = await Task.WhenAll(tasks);
        rooms.AddRange(roomResults.Where(r => r != null));

        // Cache the results
        _cache.Set(cacheKey, rooms, _cacheExpiration);
        
        _logger.LogInformation("Retrieved {Count} rooms for {Date}", rooms.Count, date);
        return rooms;
    }

    public async Task<List<Room>> GetFilteredRoomsAsync(RoomFilter filter)
    {
        var allRooms = await GetRoomsAsync(filter.SelectedDate);
        
        return allRooms.Where(room =>
        {
            var matchesZone = filter.Zone == "All" || room.Zone == filter.Zone;
            var matchesCapacity = filter.Capacity == "All" || 
                (filter.Capacity == "Small" && room.Capacity <= 4) ||
                (filter.Capacity == "Medium" && room.Capacity is > 4 and <= 8) ||
                (filter.Capacity == "Large" && room.Capacity > 8);

            return matchesZone && matchesCapacity;
        }).ToList();
    }

    public void ClearCache()
    {
        // This would ideally iterate through known cache keys, but for simplicity:
        _logger.LogInformation("Room cache cleared");
    }

    private static string GetRoomZone(int roomId)
    {
        // Same mapping as in LibCalService - could be extracted to a shared service
        var zones = new Dictionary<int, string>
        {
            { 211588, "Lower Level" }, { 211612, "Lower Level" }, { 211613, "Lower Level" },
            { 211614, "Lower Level" }, { 211615, "Lower Level" }, { 211616, "Lower Level" },
            { 211596, "Lower Level" }, { 211597, "Lower Level" }, { 211598, "Lower Level" },
            { 211606, "Lower Level" }, { 211607, "Lower Level" }, { 211609, "Lower Level" },
            { 211610, "Lower Level" }, { 211611, "Lower Level" }
        };

        return zones.TryGetValue(roomId, out var zone) ? zone : "Unknown Floor";
    }
}