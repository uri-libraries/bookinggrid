using BookGrid.Models;
using System.Net;
using System.Text.Json;
using System.Linq;

namespace BookGrid.Services;

public class LibCalService
{
    private readonly HttpClient _httpClient;
    private readonly TokenManager _tokenManager;
    private readonly ILogger<LibCalService> _logger;
    private readonly string _locationId;

    public LibCalService(
        HttpClient httpClient,
        TokenManager tokenManager,
        ILogger<LibCalService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _tokenManager = tokenManager;
        _logger = logger;
        _locationId = configuration["LibCal:LocationId"] ?? "";
    }

    public async Task<List<Location>> GetLocationsAsync()
    {
        try
        {
            var token = await _tokenManager.GetValidTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var response = await _httpClient.GetAsync("locations");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized response for locations, attempting token refresh");
                await _tokenManager.RefreshTokenAsync();
                return await GetLocationsAsync(); // Retry once
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch locations: {StatusCode}", response.StatusCode);
                return new List<Location>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var locations = JsonSerializer.Deserialize<List<Location>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Location>();

            _logger.LogInformation("Retrieved {Count} locations", locations.Count);
            return locations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching locations");
            return new List<Location>();
        }
    }

    public async Task<List<Space>> GetSpacesAsync(string locationId)
    {
        try
        {
            var token = await _tokenManager.GetValidTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var response = await _httpClient.GetAsync($"space/locations/{locationId}");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized response for spaces, attempting token refresh");
                await _tokenManager.RefreshTokenAsync();
                return await GetSpacesAsync(locationId); // Retry once
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch spaces for location {LocationId}: {StatusCode}", locationId, response.StatusCode);
                return new List<Space>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var spaces = JsonSerializer.Deserialize<List<Space>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Space>();

            _logger.LogInformation("Retrieved {Count} spaces for location {LocationId}", spaces.Count, locationId);
            return spaces;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching spaces for location {LocationId}", locationId);
            return new List<Space>();
        }
    }

    public async Task<AvailabilityResponse> GetAvailabilityAsync(DateTime date, string? locationId = null)
    {
        try
        {
            var token = await _tokenManager.GetValidTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var dateStr = date.ToString("yyyy-MM-dd");
            var queryParams = $"?date={dateStr}";
            if (!string.IsNullOrEmpty(locationId))
            {
                queryParams += $"&lid={locationId}";
            }

            var response = await _httpClient.GetAsync($"space/availability{queryParams}");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized response for availability, attempting token refresh");
                await _tokenManager.RefreshTokenAsync();
                return await GetAvailabilityAsync(date, locationId); // Retry once
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch availability: {StatusCode}", response.StatusCode);
                return new AvailabilityResponse { Availability = new List<RoomAvailability>() };
            }

            var content = await response.Content.ReadAsStringAsync();
            var availability = JsonSerializer.Deserialize<AvailabilityResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new AvailabilityResponse { Availability = new List<RoomAvailability>() };

            _logger.LogInformation("Retrieved availability for {Date}", date);
            return availability;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching availability for {Date}", date);
            return new AvailabilityResponse { Availability = new List<RoomAvailability>() };
        }
    }

    public async Task<BookingResult> CancelBookingAsync(int bookingId)
    {
        try
        {
            var token = await _tokenManager.GetValidTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var response = await _httpClient.DeleteAsync($"space/booking/{bookingId}");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized response for cancellation, attempting token refresh");
                await _tokenManager.RefreshTokenAsync();
                return await CancelBookingAsync(bookingId); // Retry once
            }

            var content = await response.Content.ReadAsStringAsync();
            var success = response.IsSuccessStatusCode;

            _logger.LogInformation("Booking cancellation for ID {BookingId}: {Success}", bookingId, success);

            return new BookingResult
            {
                Success = success,
                Message = success ? "Booking cancelled successfully" : $"Cancellation failed: {content}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking {BookingId}", bookingId);
            return new BookingResult
            {
                Success = false,
                Message = $"Error cancelling booking: {ex.Message}"
            };
        }
    }

    public async Task<List<Booking>> GetBookingsAsync(DateTime date)
    {
        try
        {
            var token = await _tokenManager.GetValidTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var dateStr = date.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync($"space/bookings?lid={_locationId}&date={dateStr}");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized response, attempting token refresh");
                await _tokenManager.RefreshTokenAsync();
                return await GetBookingsAsync(date); // Retry once
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch bookings: {StatusCode}", response.StatusCode);
                return new List<Booking>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var bookings = JsonSerializer.Deserialize<List<Booking>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Booking>();

            _logger.LogInformation("Retrieved {Count} bookings for {Date}", bookings.Count, date);
            return bookings.Where(b => b.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching bookings for {Date}", date);
            return new List<Booking>();
        }
    }

    public async Task<Room?> GetRoomDetailsAsync(int roomId, DateTime date)
    {
        try
        {
            var token = await _tokenManager.GetValidTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var dateStr = date.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync($"space/item/{roomId}?availability={dateStr}");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized response for room {RoomId}, attempting token refresh", roomId);
                await _tokenManager.RefreshTokenAsync();
                return await GetRoomDetailsAsync(roomId, date); // Retry once
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch room {RoomId}: {StatusCode}", roomId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Raw JSON response for room {RoomId}: {Content}", roomId, content);
            
            // LibCal API returns an array of room objects, so we need to deserialize as array and take the first item
            var rooms = JsonSerializer.Deserialize<Room[]>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            var room = rooms?.FirstOrDefault();

            if (room != null)
            {
                room.Zone = GetRoomZone(roomId);
                _logger.LogInformation("Retrieved details for room {RoomId}", roomId);
            }

            return room;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching room details for {RoomId}", roomId);
            return null;
        }
    }

    public async Task<BookingResult> CreateBookingAsync(BookingRequest request)
    {
        try
        {
            var token = await _tokenManager.GetValidTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var bookingData = new
            {
                start = request.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                end = request.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
                eid = request.RoomId,
                firstname = request.FirstName,
                lastname = request.LastName,
                email = request.Email
            };

            var response = await _httpClient.PostAsJsonAsync("space/booking", bookingData);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized response for booking, attempting token refresh");
                await _tokenManager.RefreshTokenAsync();
                return await CreateBookingAsync(request); // Retry once
            }

            var content = await response.Content.ReadAsStringAsync();
            var success = response.IsSuccessStatusCode;

            _logger.LogInformation("Booking attempt for room {RoomId}: {Success}", request.RoomId, success);

            return new BookingResult
            {
                Success = success,
                Message = success ? "Booking created successfully" : $"Booking failed: {content}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking for room {RoomId}", request.RoomId);
            return new BookingResult
            {
                Success = false,
                Message = $"Error creating booking: {ex.Message}"
            };
        }
    }

    private static string GetRoomZone(int roomId)
    {
        // Room zone mapping - you can move this to configuration
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