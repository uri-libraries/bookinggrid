namespace BookGrid.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public List<TimeSegment> Availability { get; set; } = new();
    public List<Booking> Bookings { get; set; } = new();
    public bool IsAvailableNow => Availability.Any(a => a.IsCurrentlyActive);
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

public class RoomFilter
{
    public string Zone { get; set; } = "All";
    public string Capacity { get; set; } = "All";
    public DateTime SelectedDate { get; set; } = DateTime.Today;
}