using System.Text.Json.Serialization;
using System.Linq;

namespace BookGrid.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TermsAndConditions { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public int FormId { get; set; }
    public bool IsBookableAsWhole { get; set; }
    public bool IsEventLocation { get; set; }
    public int ZoneId { get; set; }
    public bool Google { get; set; }
    public bool Exchange { get; set; }
    
    [JsonPropertyName("filter_ids")]
    public List<int> FilterIds { get; set; } = new();
    
    [JsonPropertyName("availability")]
    public List<AvailabilitySlot> AvailabilitySlots { get; set; } = new();
    
    // For compatibility with existing UI code
    [JsonIgnore]
    public List<TimeSegment> Availability => AvailabilitySlots.Select(slot => new TimeSegment
    {
        From = slot.From,
        To = slot.To,
        Status = "Available"  // LibCal only returns available slots
    }).ToList();
    
    // Computed properties for backwards compatibility
    public string Zone { get; set; } = string.Empty;
    public List<Booking> Bookings { get; set; } = new();
    public bool IsAvailableNow => Availability.Any(a => a.IsCurrentlyActive);
}

public class AvailabilitySlot
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public bool IsAvailable => true; // LibCal only returns available slots
    public bool IsCurrentlyActive
    {
        get
        {
            var now = DateTime.Now;
            return From <= now && now <= To;
        }
    }
}

public class TimeSegment
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsAvailable => Status.Equals("Available", StringComparison.OrdinalIgnoreCase);
    public bool IsCurrentlyActive
    {
        get
        {
            var now = DateTime.Now;
            return From <= now && now <= To && IsAvailable;
        }
    }
}

public class Booking
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class BookingRequest
{
    public int RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TimeSpan Duration => EndTime - StartTime;
}

public class BookingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? BookingId { get; set; }
}

public class LibCalTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public DateTime ExpiryTime { get; set; }
}

public class AuthServiceTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("link_id")]
    public string LinkId { get; set; } = string.Empty;
}

public class RoomFilter
{
    public string Zone { get; set; } = "All";
    public string Capacity { get; set; } = "All";
    public DateTime SelectedDate { get; set; } = DateTime.Today;
}

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Active { get; set; }
}

public class Space
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public int? Capacity { get; set; }
    public bool Active { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class AvailabilityResponse
{
    public List<RoomAvailability> Availability { get; set; } = new();
}

public class RoomAvailability
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public List<AvailabilitySlot> Slots { get; set; } = new();
}